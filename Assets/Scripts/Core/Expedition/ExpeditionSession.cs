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
    public static int PlayerCurrentHP { get; private set; }
    public static bool HasPlayerHealth { get; private set; }

    private static readonly List<string> acquiredItems = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        IsActive = false;
        CurrentFloor = FirstFloor;
        ElapsedTicks = 0f;
        LastResult = null;
        PlayerCurrentHP = 0;
        HasPlayerHealth = false;
        acquiredItems.Clear();
    }

    public static void BeginNewRun()
    {
        M3MeasurementAutomation.PrepareNewSession();
        LootRuntimeState.BeginNewRun();
        IsActive = true;
        CurrentFloor = FirstFloor;
        ElapsedTicks = 0f;
        LastResult = null;
        PlayerCurrentHP = 0;
        HasPlayerHealth = false;
        acquiredItems.Clear();
    }

    public static void RecordElapsedTicks(float elapsedTicks)
    {
        if (IsActive && elapsedTicks > ElapsedTicks) ElapsedTicks = elapsedTicks;
    }

    public static void RecordPlayerHealth(int currentHP)
    {
        if (!IsActive) return;
        PlayerCurrentHP = Mathf.Max(0, currentHP);
        HasPlayerHealth = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static void DebugSetElapsedTicks(float elapsedTicks)
    {
        if (IsActive) ElapsedTicks = Mathf.Max(0f, elapsedTicks);
    }

    public static void DebugSetCurrentFloor(int floor)
    {
        if (IsActive) CurrentFloor = Mathf.Clamp(floor, FirstFloor, FinalFloor);
    }
#endif

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
        M3MeasurementAutomation.Finish(result, ElapsedTicks, CurrentFloor);
        LootRuntimeState.ResolveRun(result);
        IsActive = false;
    }
}
