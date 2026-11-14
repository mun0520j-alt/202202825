using System;
using UnityEngine;

// 원정에서 일어난 사실만 기록한다. 퀘스트·보상 규칙은 이 상태를 직접 소유하지 않는다.
public static class ExpeditionRunRecord
{
    public static bool BossDefeated { get; private set; }
    public static ExpeditionRunSnapshot LastCompleted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        BossDefeated = false;
        LastCompleted = null;
    }

    public static void BeginRun()
    {
        BossDefeated = false;
    }

    public static void RecordBossDefeated()
    {
        if (!ExpeditionSession.IsActive) return;
        BossDefeated = true;
        Debug.Log("[RunRecord] 5층 보스 처치 기록");
    }

    public static ExpeditionRunSnapshot Complete(ExpeditionResult result, int reachedFloor, float elapsedTicks)
    {
        LastCompleted = new ExpeditionRunSnapshot
        {
            Result = result,
            ReachedFloor = reachedFloor,
            ElapsedTicks = elapsedTicks,
            BossDefeated = BossDefeated
        };
        return LastCompleted;
    }
}

[Serializable]
public sealed class ExpeditionRunSnapshot
{
    public ExpeditionResult Result;
    public int ReachedFloor;
    public float ElapsedTicks;
    public bool BossDefeated;
}
