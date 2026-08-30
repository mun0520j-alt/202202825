using System;
using UnityEngine;

// ================================================================================================
// [Summary] DungeonClock
// TickManager가 발행하는 "이번에 tick이 얼마나 소비됐는지" 이벤트(OnTimeAdvanced)를 구독해서
// 누적하는 역할만 담당한다. 두 가지 결과물을 만든다:
//   1) 던전 안에서 지금 몇 시인지("HH:mm" 문자열, GetClockString())
//   2) 288tick(=24시간) 넘게 던전에 있으면 MIA(실종/귀환 실패) 판정
//
// 책임 분리가 중요한 이유: TickManager는 "누구 차례인지 / 큐 순서"만 관리하고, 이 클래스는
// "그래서 지금 게임 내 시간이 몇 시인지"만 관리한다 — TickManager를 봐도 시간 개념이 안 보이고,
// DungeonClock을 봐도 턴 순서 개념이 안 보이는 게 정상이다(단일 책임 원칙).
//
// [향후 확장] 월식의 제단(ALTAR_AND_TIME_SYSTEM_DESIGN.md 4장) 사용 시 던전 "표시" 시간을
// 자정으로 고정하는 기능을 위해 LockDisplayToMidnight() 확장 포인트를 미리 남겨둔다. ALTAR 문서
// 원칙("Tick이 없어지는 게 아님, World Time은 그대로 진행되고 Dungeon의 Time State만 고정")대로,
// ElapsedTicks 누적이나 MIA 판정에는 전혀 영향을 안 주고 "화면에 뭐라고 찍히는지"만 고정한다.
// ================================================================================================
public class DungeonClock : MonoBehaviour
{
    // "MIA 판정까지 필요한 tick 총량(임계값)" — 이름에 Per가 붙어서 "~당 비율"처럼 보일 수 있는데
    // 그런 뜻이 아니다. "MIA 하나 찍는 데 필요한 tick 수"라기보다는 "이 값 이상 쌓이면 MIA"라는
    // 상한선(threshold)이라서, 이름만 보고 헷갈리지 않게 Threshold로 바꿨다.
    // 5분/tick * 288tick = 1440분 = 24시간이라 값 자체는 그대로 유지.
    public const float MIAThresholdTicks = 288f; // 24시간 = 288tick

    // "tick 하나가 게임 내 시간으로 몇 분인지"의 환산 비율. 이건 진짜 "~당(Per)" 관계라 이름 그대로 둔다.
    public const float MinutesPerTick = 5f;

    [Tooltip("던전 출격 시각(시). 기본 06:00 — MAP_GENERATOR_DESIGN.md 예시 기준.")]
    [SerializeField] private int departureHour = 6;

    // "던전 진입 후 지금까지 누적된 tick 수"를 뜻한다(=Elapsed: 경과했다/흘러갔다).
    // 매 CompleteTurn마다 TickManager가 OnTimeAdvanced로 소비량을 보고하면 그만큼씩 여기 더해진다.
    // 예) 5칸 이동(0.2 tick * 5) 하면 ElapsedTicks가 1.0만큼 늘어남 = 게임 내 5분 경과.
    public float ElapsedTicks { get; private set; }

    // MIAThresholdTicks(288tick=24h) 이상 누적되면 true로 바뀐다 — "던전에서 못 돌아왔다" 판정.
    public bool IsMIA { get; private set; }

    // 월식의 제단 등으로 "화면에 찍히는 시각"이 자정으로 강제 고정됐는지 여부(추후 기능 —
    // 지금은 아무도 LockDisplayToMidnight()을 안 부르니 항상 false).
    public bool IsDisplayLocked { get; private set; }

    // MIA 판정이 발생한 순간 한 번 호출된다(구독자 없어도 안전 — ?.Invoke).
    public event Action OnMIA;

    // tick이 누적될 때마다(=시간이 흐를 때마다) 호출된다. 인자는 갱신된 ElapsedTicks 값.
    public event Action<float> OnClockChanged;

    private void OnEnable()
    {
        if (TickManager.Instance != null)
        {
            // 혹시 모를 중복 구독을 막기 위해 구독 전에 한 번 해지부터 한다 — 이미 구독 안 돼있는
            // 상태에서 -=를 해도 C# 델리게이트는 에러 없이 그냥 무시하니 비용 없는 안전장치다.
            // (Unity의 OnEnable/OnDisable은 원래 항상 쌍으로 호출되게 보장되긴 하지만, RegisterActor의
            // 중복 등록 가드처럼 방어적으로 통일해둔다.)
            TickManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            TickManager.Instance.OnTimeAdvanced += HandleTimeAdvanced;
        }
        else
        {
            Debug.LogWarning("[DungeonClock] TickManager.Instance가 아직 없어서 구독을 못 했습니다 — " +
                              "씬에서 TickManager가 DungeonClock보다 먼저 초기화되는지 확인해주세요.");
        }
    }

    private void OnDisable()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
        }
    }

    // 던전 진입(런 시작) 시 호출 — 누적치/MIA/표시잠금 상태를 전부 초기값으로 되돌린다.
    public void ResetForNewRun()
    {
        ElapsedTicks = 0f;
        IsMIA = false;
        IsDisplayLocked = false;
    }

    // 월식의 제단 전용 확장 포인트 — 표시 시각만 자정(00:00)으로 고정한다.
    // 원래는 임의의 시(hour)를 받는 LockDisplayToHour(int hour)였는데, 실제로 이 기능을 쓰는 곳이
    // "항상 자정 고정"뿐이라(ALTAR_AND_TIME_SYSTEM_DESIGN.md) 굳이 아무 시각이나 받게 열어두는 게
    // 과설계라고 판단해서 자정 전용으로 단순화했다(2026-08-27 리뷰 반영).
    public void LockDisplayToMidnight()
    {
        IsDisplayLocked = true;
    }

    // TickManager.OnTimeAdvanced 구독 콜백 — "이번 행동에 tick이 얼마나 소비됐는지(cost)"를 받아서
    // 누적하고, 구독자들에게 갱신을 알리고, 임계값을 넘었으면 MIA를 발생시킨다.
    private void HandleTimeAdvanced(float cost)
    {
        // 이미 MIA 판정이 난 뒤에는(예: 이미 24시간 넘겨서 못 돌아온 상태) 더 쌓아봤자 의미가
        // 없어서 여기서 조용히 무시한다 — OnMIA가 중복으로 여러 번 발생하는 것도 막아준다.
        if (IsMIA) return;

        ElapsedTicks += cost;
        OnClockChanged?.Invoke(ElapsedTicks);

        if (ElapsedTicks >= MIAThresholdTicks)
        {
            IsMIA = true;
            OnMIA?.Invoke();
        }
    }

    // "HH:mm" 형식 문자열로 변환 — 지금은 TickQueueTest 콘솔 로그에서만 쓰이지만, 나중에 던전
    // HUD에 실시간 시계로 그대로 붙일 수 있게 설계했다(콘솔 전용 헬퍼가 아님).
    // IsDisplayLocked 상태면 실제 경과(ElapsedTicks)와 무관하게 항상 자정만 보여준다.
    public string GetClockString()
    {
        if (IsDisplayLocked)
        {
            return "00:00"; // 월식의 제단으로 자정 고정된 상태
        }

        float totalMinutes = departureHour * 60f + ElapsedTicks * MinutesPerTick;
        int minutesOfDay = Mathf.FloorToInt(totalMinutes) % (24 * 60);
        int hour = minutesOfDay / 60;
        int minute = minutesOfDay % 60;
        return $"{hour:00}:{minute:00}";
    }

    // MIA까지 남은 tick 수 — 나중에 "귀환 남은 시간 XX분" 같은 UI에 바로 쓸 수 있게 미리 노출.
    public float RemainingTicks => Mathf.Max(0f, MIAThresholdTicks - ElapsedTicks);
}
