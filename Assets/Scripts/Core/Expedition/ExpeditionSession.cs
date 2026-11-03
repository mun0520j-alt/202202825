using System.Collections.Generic;
using UnityEngine;

// 씬을 다시 불러도 한 원정의 층/Tick/결과가 유지되는 런타임 세션이다.
public static class ExpeditionSession
{
    public const int FirstFloor = 1;
    public const int FinalFloor = 3;

    public static bool IsActive { get; private set; }
    public static int CurrentFloor { get; private set; } = FirstFloor;
    public static float ElapsedTicks { get; private set; }
    public static ExpeditionResultData LastResult { get; private set; }

    private static readonly List<string> acquiredItems = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        IsActive = false;
        CurrentFloor = FirstFloor;
        ElapsedTicks = 0f;
        LastResult = null;
        acquiredItems.Clear();
    }

    public static void BeginNewRun()
    {
        IsActive = true;
        CurrentFloor = FirstFloor;
        ElapsedTicks = 0f;
        LastResult = null;
        acquiredItems.Clear();
    }

    public static void RecordElapsedTicks(float elapsedTicks)
    {
        if (IsActive && elapsedTicks > ElapsedTicks) ElapsedTicks = elapsedTicks;
    }

    public static bool TryAdvanceFloor()
    {
        if (!IsActive || CurrentFloor >= FinalFloor) return false;
        CurrentFloor++;
        return true;
    }

    public static void Complete(ExpeditionResult result)
    {
        if (!IsActive) return;

        LastResult = new ExpeditionResultData
        {
            Result = result,
            ElapsedTicks = ElapsedTicks,
            ReachedFloor = CurrentFloor,
            AcquiredItems = new List<string>(acquiredItems)
        };
        IsActive = false;
    }
}
