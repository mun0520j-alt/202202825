using System;
using System.Collections.Generic;
using UnityEngine;

// Dungeon Tools 7) Tick 시스템의 큐 기반 스케줄러 (HOMEPC_SYNC_NOTES.md 2절에서 확정된 모델).
// "매 틱마다 전체 브로드캐스트"가 아니라, 등록된 액터 중 "다음 행동 시각"이 가장 이른 액터
// 딱 한 명에게만 OnTurnStart()를 콜백한다.
//
// 행동의 tick 비용 계산(스탯/장비/이동 배율 등)은 이 클래스 책임이 아니다 — 액터가
// CompleteTurn()으로 직접 비용을 보고한다. 그래서 나중에 "빠른 몹"이나 "이동속도 아이템" 같은
// 걸 붙여도 이 클래스는 수정할 필요가 없다(TickCost.cs 참고).
//
// DungeonScene 전용 오브젝트로 존재한다(설계 확정) — BaseCampScene에는 이 컴포넌트 자체가 없음.
public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    // 액터가 행동을 완료 보고할 때마다 발행 — DungeonClock 등 "누적 시간이 필요한 쪽"이 구독해서 쓴다.
    // 인자: 이번에 소비된 tick 비용.
    public event Action<float> OnTimeAdvanced;

    [Header("워치독 (개발 중 실수 감지용 — 게임 로직을 멈추거나 자동으로 넘기지 않음)")]
    [Tooltip("현재 차례인 액터가 SuppressStuckTurnWarning=false인데 이 시간(초, Time.deltaTime 누적 기준 — " +
             "일시정지 중에는 안 흐름) 안에 CompleteTurn을 호출하지 않으면 버그 리포트 로그(LogError)를 " +
             "찍는다. 어디까지나 'CompleteTurn 호출을 빠뜨린 액터'를 빨리 찾기 위한 디버그용 — 자동 복구는 하지 않는다.")]
    [SerializeField] private float stuckTurnBugReportSeconds = 10f;

    // 액터 하나가 스케줄 안에서 어떤 상태인지 — "다음에 행동할 시각"만 들고 있는 순수 데이터.
    private class ScheduledActor
    {
        public ITurnActor Actor;
        public float NextActionTime;
    }

    private readonly List<ScheduledActor> registeredActors = new List<ScheduledActor>();
    private ScheduledActor currentTurnActor;
    private bool schedulerRunning;
    private float currentTurnElapsedSeconds; // Time.deltaTime 누적 — 일시정지(Time.timeScale=0) 중엔 안 늘어남
    private bool warnedAboutCurrentTurn;

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

    // 현재 차례인 액터가 행동을 끝냈을 때 호출. cost만큼 그 액터의 다음 행동 시각을 미룬다.
    //
    // 다음 액터로 넘어가는 처리는 여기서 직접(재귀적으로) 하지 않는다 — 대신 지금 이 호출이
    // AdvanceSchedule()의 반복문 "안에서" 온 건지(=AI처럼 OnTurnStart 안에서 즉시 완료 보고) 아니면
    // 그 반복문 "밖에서" 온 건지(=플레이어 입력 콜백처럼 나중에 비동기로 완료 보고) 구분해서:
    //   - 반복문 안이면: 플래그만 세우고 그 반복문이 이어서 처리하게 한다(재귀 안 함)
    //   - 반복문 밖이면: 새 반복문을 시작해도 스택이 안전하니 AdvanceSchedule()을 새로 호출한다
    public void CompleteTurn(ITurnActor actor, float cost)
    {
        if (currentTurnActor == null || currentTurnActor.Actor != actor)
        {
            Debug.LogWarning($"[TickManager] {actor}가 자기 차례가 아닌데 CompleteTurn을 호출했습니다 — 무시.");
            return;
        }

        currentTurnActor.NextActionTime += cost;
        OnTimeAdvanced?.Invoke(cost);

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
    private void AdvanceSchedule()
    {
        isAdvancingSchedule = true;
        do
        {
            turnJustCompletedInsideLoop = false;
            currentTurnActor = FindEarliestActor();
            if (currentTurnActor == null) break; // 등록된 액터가 없으면 대기

            currentTurnElapsedSeconds = 0f;
            warnedAboutCurrentTurn = false;
            currentTurnActor.Actor.OnTurnStart();
        } while (turnJustCompletedInsideLoop);
        isAdvancingSchedule = false;
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
        if (!schedulerRunning || currentTurnActor == null || warnedAboutCurrentTurn) return;
        if (currentTurnActor.Actor.SuppressStuckTurnWarning) return;

        currentTurnElapsedSeconds += Time.deltaTime;
        if (currentTurnElapsedSeconds >= stuckTurnBugReportSeconds)
        {
            Debug.LogError($"[TickManager] 버그 리포트: {currentTurnActor.Actor}가 {stuckTurnBugReportSeconds}초 " +
                            "넘게 CompleteTurn을 호출하지 않았습니다 — 해당 액터의 OnTurnStart 구현에서 " +
                            "CompleteTurn 호출이 빠졌는지 확인해주세요.");
            warnedAboutCurrentTurn = true; // 한 번만 보고
        }
    }
}
