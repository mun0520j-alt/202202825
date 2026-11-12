using UnityEngine;
using UnityEngine.UI;

// 모든 씬 오른쪽 아래에 빌드 버전을 표시한다 (BETA 0.1~). 값은 Player Settings의 Version(bundleVersion).
// 피드백 스크린샷에 버전이 함께 찍히도록 항상 맨 위에 그린다.
public static class BuildVersionLabel
{
    public const string Prefix = "BETA ";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var root = new GameObject("BuildVersionLabel", typeof(Canvas));
        Object.DontDestroyOnLoad(root);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-8f, 4f);
        rect.sizeDelta = new Vector2(200f, 20f);

        Text text = textObject.GetComponent<Text>();
        text.font = HudFont.GetDefault();
        text.fontSize = 12;
        text.alignment = TextAnchor.LowerRight;
        text.color = new Color(1f, 1f, 1f, 0.55f);
        text.raycastTarget = false;
        text.text = Prefix + Application.version;
    }
}
