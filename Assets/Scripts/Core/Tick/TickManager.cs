using System;
using System.Collections.Generic;
using UnityEngine;

// ================================================================================================
// [Summary] TickManager
// Tick 시스템의 큐 기반 스케줄러 (HOMEPC_SYNC_NOTES.md 2절에서 확정된 모델).
// "매 틱마다 전체 브로드캐스트"가 아니라, 등록된 액터 중 "다음 행동 시각"이 가장 이른 액터
// 딱 한 명에게만 OnTurnStart()를 콜백한다 — 그래서 몹이 수십 마리로 늘어나도 매 틱마다
// 전부 검사하는 게 아니라 "다음 차례 한 명"만 찾으면 돼서 확장에 안전하다.
//
// 행동의 tick 비용 계산(스탯/장비/이동 배율 등)은 이 클래스 책임이 아니다 — 액터가
// CompleteTurn()으로 직접 비용을 보고한다. 그래서 나중에 "빠른 몹"이나 "이동속도 아이템" 같은
// 걸 붙여도 이 클래스는 수정할 필요가 없다(TickCost.cs 참고).
//
// 핵심 흐름 요약: RegisterActor로 등록 → BeginSchedule로 시작 → FindEarliestActor가 다음
// 차례를 찾아서 OnTurnStart 콜백 → 그 액터가 행동을 정하면 CompleteTurn 호출 → 다시
// FindEarliestActor로 돌아감(AdvanceSchedule이 이 반복을 재귀 없이 처리, 아래 설명 참고).
//
// DungeonScene 전용 오브젝트로 존재한다(설계 확정) — BaseCampScene에는 이 컴포넌트 자체가 없음.
// ================================================================================================
public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    // 액터가 행동을 완료 보고할 때마다 발행 — DungeonClock 등 "누적 시간이 필요한 쪽"이 구독해서 쓴다.
    // 인자: 이번에 소비된 tick 비용.
    public event Action<float> OnTimeAdvanced;

    // ============================================================================================
    // [주의] 이 클래스 안에는 시간 개념이 두 종류 있고 서로 완전히 무관하다 — 헷갈리지 말 것.
    //   1) "tick 시간"(ScheduledActor.NextActionTime) — 게임 로직 상의 턴 순서 정렬 전용.
    //      CompleteTurn()에서 cost만큼 쌓이고, FindEarliestActor()가 이 값이 제일 작은 액터를 고른다.
    //   2) "실시간 초"(watchdogElapsedSeconds 등, 아래 워치독 그룹) — Time.deltaTime 누적, 실제
    //      벽시계 기준 몇 초 흘렀는지. 워치독(디버그용 CompleteTurn 누락 감지)에서만 쓰고,
    //      tick 스케줄링 로직에는 전혀 관여하지 않는다.
    // ============================================================================================

    [Header("워치독 (개발 중 실수 감지용 — 게임 로직을 멈추거나 자동으로 넘기지 않음)")]
    [Tooltip("현재 차례인 액터가 SuppressStuckTurnWarning=false인데 이 시간(초, Time.deltaTime 누적 기준 — " +
             "일시정지 중에는 안 흐름) 안에 CompleteTurn을 호출하지 않으면 버그 리포트 로그(LogError)를 " +
             "찍는다. 어디까지나 'CompleteTurn 호출을 빠뜨린 액터'를 빨리 찾기 위한 디버그용 — 자동 복구는 하지 않는다.")]
    [SerializeField] private float stuckTurnBugReportSeconds = 10f;

    // 액터 하나가 스케줄 안에서 어떤 상태인지 — "다음에 행동할 시각"만 들고 있는 순수 데이터.
    // NextActionTime은 tick 단위(게임 로직 시간)다 — 아래 워치독의 "실시간 초"와는 다른 값이니
    // 섞어서 생각하지 말 것(파일 상단 [주의] 참고).
    private class ScheduledActor
    {
        public ITurnActor Actor;
        public float NextActionTime;
    }

    // 지금 스케줄에 참여 중인 모든 액터 목록 — Player도 여기 하나로 들어있고, 나중에 Enemy가
    // 생기면 마리 수만큼 이 리스트에 같이 등록된다(액터 종류를 구분 안 하고 전부 동등하게 취급).
    private readonly List<ScheduledActor> registeredActors = new List<ScheduledActor>();

    // 지금 "차례가 진행 중인" 액터. null이면 아무도 차례가 아닌 상태(등록된 액터가 하나도 없거나,
    // 그 액터가 Unregister되어 사라진 직후).
    private ScheduledActor currentTurnActor;

    // BeginSchedule()이 호출된 뒤 true — 스케줄이 실제로 굴러가고 있는지 표시하는 값.
    // BeginSchedule 중복 호출을 막는 가드로도 쓰이고, Update()의 워치독이 "아직 스케줄
    // 시작도 안 했는데 감시하는" 오작동을 막는 가드로도 쓰인다.
    private bool schedulerRunning;

    // [워치독 전용 · 실시간 초] 지금 차례인 액터가 CompleteTurn을 안 부른 지 몇 초째인지.
    // Time.deltaTime을 매 프레임 누적한 값이라 실제 벽시계 초 단위다(일시정지 중엔 안 늘어남).
    // 새 턴이 시작될 때마다(AdvanceSchedule 안에서) 0으로 리셋된다. NextActionTime과는
    // 완전히 다른 값이니 절대 같은 개념으로 헷갈리지 말 것(파일 상단 [주의] 참고).
    private float watchdogElapsedSeconds;

    // [워치독 전용] 지금 차례에 대해 이미 버그 리포트(LogError)를 한 번 찍었는지 여부.
    // 이게 없으면 워치독이 threshold(10초)를 넘긴 뒤에도 매 프레임 계속 LogError를 찍어서
    // 콘솔이 도배됨 — 한 턴당 딱 한 번만 보고하게 막아주는 "이미 보고함" 플래그다.
    // 새 턴이 시작될 때마다(AdvanceSchedule 안에서) false로 리셋된다.
    private bool stuckTurnAlreadyReported;

    // 재귀 방지 가드 — AdvanceSchedule()의 반복문이 실행되는 동안인지 표시한다. AI처럼 OnTurnStart
    // 안에서 바로 CompleteTurn을 부르는 액터가 연쇄로 여러 번 이어져도(예: 몹 여러 마리가 연속으로
    // 즉시 행동), CompleteTurn → 다음 액터 호출을 재귀로 쌓지 않고 이 가드 + 아래 플래그로 한 반복문
    // 안에서 처리한다 — 재귀 깊이가 액터 수만큼 쌓여서 StackOverflow가 나는 걸 방지.
    private bool isAdvancingSchedule;
    private bool turnJustCompletedInsideLoop;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TickManager] 씬에 TickManager가 이미 있어서 중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 던전 진입 시 Player/Enemy 등이 자기 자신을 스케줄에 등록할 때 호출.
    // 같은 액터를 중복 등록하지 않도록 방어 — register/unregister 누수 방지(사용자 피드백 반영).
    public void RegisterActor(ITurnActor actor)
    {
        if (registeredActors.Exists(entry => entry.Actor == actor))
        {
            Debug.LogWarning($"[TickManager] {actor}는 이미 등록되어 있어서 중복 등록을 무시합니다.");
            return;
        }
        registeredActors.Add(new ScheduledActor { Actor = actor, NextActionTime = 0f });
    }

    // 액터 사망/비활성화/씬 언로드 시 반드시 호출해야 함 — 안 부르면 죽은 액터가 스케줄에 남아
    // 계속 차례를 받으려고 하는 누수가 생긴다.
    public void UnregisterActor(ITurnActor actor)
    {
        registeredActors.RemoveAll(entry => entry.Actor == actor);
        if (currentTurnActor != null && currentTurnActor.Actor == actor)
        {
            currentTurnActor = null;
        }
    }

    // 모든 액터 등록이 끝난 뒤(씬 초기화 완료 후) 한 번 호출해서 스케줄을 시작한다.
    // 실제 게임에서는 DungeonSceneBootstrapper가 이걸 호출한다(진짜 진입점) — TickQueueTestBootstrapper는
    // 스케줄러 자체를 검증하려고 더미 액터로 이걸 호출하는 테스트 전용 호출부다. 둘 다 같은 API를
    // 쓰지만 목적이 다르니 헷갈리지 말 것.
    public void BeginSchedule()
    {
        if (schedulerRunning)
        {
            Debug.LogWarning("[TickManager] 이미 스케줄이 시작된 상태에서 BeginSchedule이 다시 호출됐습니다 — 무시.");
            return;
        }
        schedulerRunning = true;
        AdvanceSchedule();
    }

    // 현재 차례인 액터가 행동을 끝냈을 때 호출. Player/Enemy 둘 다 결국 이 함수 하나로 턴을
    // 넘긴다 — 실제 게임에서 이걸 부르는 건 Player/Enemy ITurnActor 구현체뿐이다.
    //
    // tickCost 파라미터 주의: "이동 비용"이 아니라 "이번에 한 행동이 뭐였든 그 행동의 tick 비용"이다.
    // 이동이면 TickCost.PerTileMove, 공격이면 TickCost.Attack처럼 호출하는 쪽(액터)이 자기가 방금
    // 한 행동에 맞는 값을 골라서 넘겨준다 — 이 함수는 그게 이동인지 공격인지 전혀 신경 안 쓰고
    // 그냥 "이번 턴에 tickCost만큼 시간이 흘렀다"로만 취급한다(TickCost.cs 참고).
    //
    // 방어 코드: currentTurnActor가 없거나(아직 아무도 차례가 아님) actor가 지금 차례인 액터와
    // 다르면(자기 차례도 아닌데 호출함) 경고만 찍고 무시한다 — 잘못된 호출로 큐가 꼬이는 걸 막는다.
    //
    // 다음 액터로 넘어가는 처리는 여기서 직접(재귀적으로) 하지 않는다 — 대신 지금 이 호출이
    // AdvanceSchedule()의 반복문 "안에서" 온 건지(=AI처럼 OnTurnStart 안에서 즉시 완료 보고) 아니면
    // 그 반복문 "밖에서" 온 건지(=플레이어 입력 콜백처럼 나중에 비동기로 완료 보고) 구분해서:
    //   - 반복문 안이면: 플래그만 세우고 그 반복문이 이어서 처리하게 한다(재귀 안 함)
    //   - 반복문 밖이면: 새 반복문을 시작해도 스택이 안전하니 AdvanceSchedule()을 새로 호출한다
    public void CompleteTurn(ITurnActor actor, float tickCost)
    {
        if (currentTurnActor == null || currentTurnActor.Actor != actor)
        {
            Debug.LogWarning($"[TickManager] {actor}가 자기 차례가 아닌데 CompleteTurn을 호출했습니다 — 무시.");
            return;
        }

        currentTurnActor.NextActionTime += tickCost;
        OnTimeAdvanced?.Invoke(tickCost);

        if (isAdvancingSchedule)
        {
            turnJustCompletedInsideLoop = true;
            return;
        }

        AdvanceSchedule();
    }

    // "다음 액터 찾기 → OnTurnStart 호출"을 재귀가 아니라 반복문으로 처리한다. 액터가 OnTurnStart
    // 안에서 즉시 CompleteTurn을 부르면(AI 등) 이 반복문이 계속 돌면서 다음 액터로 넘어가고,
    // 액터가 나중에 비동기로 완료하면(플레이어 입력 등) 이 반복문은 한 바퀴만 돌고 자연스럽게
    // 멈춘다 — 어느 경우든 실행 스택 깊이는 항상 일정하다.
    //
    // 주의: 이 함수 자체는 게임 진행 중에 여러 번(플레이어가 움직일 때마다 등) 호출된다.
    // isAdvancingSchedule 가드가 막는 건 "총 호출 횟수"가 아니라 "한 번의 호출이 끝나기 전에
    // 스스로를 또 부르는 것(재귀 중첩)"뿐이다 — 그래서 액터 여러 명이 연쇄로 즉시 행동해도
    // 이 함수 호출 하나(그 안의 do-while 반복문)로 전부 처리되고, 스택엔 절대 안 쌓인다.
    private void AdvanceSchedule()
    {
        isAdvancingSchedule = true;
        do
        {
            turnJustCompletedInsideLoop = false;
            currentTurnActor = FindEarliestActor();
            if (currentTurnActor == null) break; // 등록된 액터가 없으면 대기

            watchdogElapsedSeconds = 0f;
            stuckTurnAlreadyReported = false;
            currentTurnActor.Actor.OnTurnStart();
        } while (turnJustCompletedInsideLoop);
        isAdvancingSchedule = false;
    }

    // [임시 위치 — 2026-08-28 결정] Player/Enemy가 같은 셀에 겹쳐 이동하는 문제(콜라이더로는
    // 못 막음, ITurnActor.CurrentCell 주석 참고)를 해결하려고 추가했다. 셀 점유 조회에는 등록된
    // 액터 목록이 필요한데 지금은 TickManager가 그 목록(registeredActors)을 유일하게 들고 있어서
    // 일단 여기 붙였다 — 사용자도 "TickManager 책임이 너무 많아진다"고 동의했고, 다음 작업으로
    // 액터 등록/조회(RegisterActor/UnregisterActor/IsCellOccupied 등)를 스케줄링과 분리된 별도
    // 클래스(예: ActorRegistry)로 뽑아내는 책임 분리 리팩터링을 예정해뒀다. 그때까지의 임시 위치.
    //
    // excludingSelf: 자기 자신이 서 있는 셀을 검사할 때(예: 제자리 유지) 자기 자신 때문에
    // "점유됨"으로 잘못 판정되지 않도록 제외한다.
    public bool IsCellOccupied(UnityEngine.Vector3Int cell, ITurnActor excludingSelf = null)
    {
        foreach (var entry in registeredActors)
        {
            if (entry.Actor == excludingSelf) continue;
            if (entry.Actor.CurrentCell == cell) return true;
        }
        return false;
    }

    private ScheduledActor FindEarliestActor()
    {
        ScheduledActor earliest = null;
        foreach (var entry in registeredActors)
        {
            if (earliest == null || entry.NextActionTime < earliest.NextActionTime)
            {
                earliest = entry;
            }
        }
        return earliest;
    }

    // 워치독: 현재 차례인 액터가 SuppressStuckTurnWarning=false인데 너무 오래 CompleteTurn을
    // 안 부르면 버그 리포트를 찍는다(자동 복구 없음) — 플레이어처럼 입력을 기다리는 액터는
    // SuppressStuckTurnWarning=true로 이 검사에서 제외된다.
    private void Update()
    {
        if (!schedulerRunning || currentTurnActor == null || stuckTurnAlreadyReported) return;
        if (currentTurnActor.Actor.SuppressStuckTurnWarning) return;

        watchdogElapsedSeconds += Time.deltaTime;
        if (watchdogElapsedSeconds >= stuckTurnBugReportSeconds)
        {
            Debug.LogError($"[TickManager] 버그 리포트: {currentTurnActor.Actor}가 {stuckTurnBugReportSeconds}초 " +
                            "넘게 CompleteTurn을 호출하지 않았습니다 — 해당 액터의 OnTurnStart 구현에서 " +
                            "CompleteTurn 호출이 빠졌는지 확인해주세요.");
            stuckTurnAlreadyReported = true; // 한 번만 보고
        }
    }
}
