using System;
using UnityEngine;

// Dungeon Tools 7) TickManager가 보고하는 tick 소비량을 누적해서 "실제 시:분" 표시와
// 288tick(24시간) MIA 판정을 담당한다. 스케줄링 로직(TickManager)과는 분리되어 있다 — 이 클래스는
// TickManager.OnTimeAdvanced를 구독만 하고, 누구의 차례인지/큐 관리에는 전혀 관여하지 않는다.
//
// 월식의 제단(추후 기능, ALTAR_AND_TIME_SYSTEM_DESIGN.md 4장 참고) 사용 시 던전 시간을 자정으로
// "표시상" 고정하는 기능을 위해 LockDisplayToHour() 확장 포인트를 미리 남겨둔다 — 이번 단계에서는
// 아무도 호출하지 않는다. ALTAR 문서 원칙("Tick이 없어지는 게 아님, World Time은 그대로 진행되고
// Dungeon의 Time State만 고정") 그대로, ElapsedTicks/MIA 판정에는 전혀 영향을 안 주고 표시만 고정한다.
public class DungeonClock : MonoBehaviour
{
    public const float TicksPerMIA = 288f; // 24시간
    public const float MinutesPerTick = 5f;

    [Tooltip("던전 출격 시각(시). 기본 06:00 — MAP_GENERATOR_DESIGN.md 예시 기준.")]
    [SerializeField] private int departureHour = 6;

    public float ElapsedTicks { get; private set; }
    public bool IsMIA { get; private set; }

    // 월식의 제단 등으로 시간대 표시가 강제 고정됐는지 여부 (추후 기능 — 지금은 항상 false).
    public bool IsDisplayLocked { get; private set; }
    private int lockedDisplayHour;

    public event Action OnMIA;
    public event Action<float> OnClockChanged; // 인자: ElapsedTicks

    private void OnEnable()
    {
        if (TickManager.Instance != null)
        {
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

    public void ResetForNewRun()
    {
        ElapsedTicks = 0f;
        IsMIA = false;
        IsDisplayLocked = false;
    }

    // 추후 월식의 제단 기능에서 호출할 확장 포인트 — 시:분 표시만 특정 시각으로 고정한다.
    public void LockDisplayToHour(int hour)
    {
        IsDisplayLocked = true;
        lockedDisplayHour = hour;
    }

    private void HandleTimeAdvanced(float cost)
    {
        if (IsMIA) return;

        ElapsedTicks += cost;
        OnClockChanged?.Invoke(ElapsedTicks);

        if (ElapsedTicks >= TicksPerMIA)
        {
            IsMIA = true;
            OnMIA?.Invoke();
        }
    }

    // UI 표시용 "HH:mm" 문자열. IsDisplayLocked면 실제 경과와 무관하게 고정된 시각만 보여준다.
    public string GetClockString()
    {
        if (IsDisplayLocked)
        {
            return $"{lockedDisplayHour:00}:00";
        }

        float totalMinutes = departureHour * 60f + ElapsedTicks * MinutesPerTick;
        int minutesOfDay = Mathf.FloorToInt(totalMinutes) % (24 * 60);
        int hour = minutesOfDay / 60;
        int minute = minutesOfDay % 60;
        return $"{hour:00}:{minute:00}";
    }

    public float RemainingTicks => Mathf.Max(0f, TicksPerMIA - ElapsedTicks);
}
