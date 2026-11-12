using UnityEngine;

// 모든 씬 오른쪽 아래에 빌드 버전을 표시한다 (BETA 0.1~). 값은 Player Settings의 Version(bundleVersion).
// 주의: Canvas를 만들면 ScreenSpaceCanvasProvider의 FindObjectOfType<Canvas>()가 이 Canvas를 HUD 루트로
// 잡아 GraphicRaycaster 없는 Canvas에 UI가 붙고 모든 버튼 클릭이 막힌다(2026-11-12). 그래서 OnGUI로 그린다.
public sealed class BuildVersionLabel : MonoBehaviour
{
    public const string Prefix = "BETA ";
    private GUIStyle style;
    private string text;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("BuildVersionLabel");
        DontDestroyOnLoad(go);
        go.AddComponent<BuildVersionLabel>();
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.LowerRight };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            text = Prefix + Application.version;
        }
        GUI.depth = -1000;
        GUI.Label(new Rect(Screen.width - 208f, Screen.height - 24f, 200f, 20f), text, style);
    }
}
