using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 파일에 수동 컴포넌트를 추가하지 않아도 BaseCamp 임시 화면을 항상 준비한다.
public static class ExpeditionRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInitialScene()
    {
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != "BaseCamp" || Object.FindObjectOfType<ExpeditionResultPanel>() != null) return;
        new GameObject("ExpeditionResultPanel(AutoCreated)").AddComponent<ExpeditionResultPanel>();
    }
}
