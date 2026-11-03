#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// M1 경계값 QA용. 에디터 또는 Development Build에만 포함된다.
public sealed class ExpeditionDebugCommands : MonoBehaviour
{
    [Min(0f)] public float targetElapsedTicks = 1439f;

    [ContextMenu("QA/Set Elapsed Tick")]
    public void SetConfiguredTick()
    {
        SetElapsedTicks(targetElapsedTicks);
    }

    public static bool SetElapsedTicks(float elapsedTicks)
    {
        var tickManager = TickManager.Instance;
        var dungeonClock = tickManager != null ? tickManager.GetComponent<DungeonClock>() : null;
        if (tickManager == null || dungeonClock == null || !ExpeditionSession.IsActive)
        {
            Debug.LogWarning("[QA] 활성 원정의 TickManager/DungeonClock을 찾지 못해 Tick을 설정하지 못했습니다.");
            return false;
        }

        float safeTick = Mathf.Max(0f, elapsedTicks);
        tickManager.DebugSetTimeline(safeTick);
        dungeonClock.DebugSetElapsedTicks(safeTick);
        ExpeditionSession.DebugSetElapsedTicks(safeTick);
        Debug.Log($"[QA] 누적 Tick을 {safeTick:0.##}(으)로 설정했습니다.");
        return true;
    }
}
#endif
