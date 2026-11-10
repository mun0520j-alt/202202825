using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// M1용 임시 영지/결과 화면. 영지에서는 TickManager를 만들지 않아 시간이 흐르지 않는다.
public sealed class ExpeditionResultPanel : MonoBehaviour
{
    private void Start()
    {
        Build();
    }

    private void Build()
    {
        Transform root = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();

        var panel = CreateUiObject("M1ResultPanel", root);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(680f, 520f);

        string body;
        var result = ExpeditionSession.LastResult;
        if (result == null)
        {
            body = "원정 준비\n\n1층부터 3층까지 내려가 탈출하세요.";
        }
        else
        {
            string resultName = result.Result == ExpeditionResult.Escaped ? "생환"
                : result.Result == ExpeditionResult.Dead ? "사망" : "MIA";
            body = $"원정 결과: {resultName}\n\n경과 Tick: {result.ElapsedTicks:0}\n도달 층: {result.ReachedFloor}층";
        }

        string recoveryNotice = M5SaveService.ConsumeRecoveryNotice();
        if (!string.IsNullOrEmpty(recoveryNotice)) body = recoveryNotice + "\n\n" + body;

        var textGo = CreateUiObject("ResultText", panel.transform);
        var text = textGo.AddComponent<Text>();
        text.font = HudFont.GetDefault();
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = body;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.25f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        var buttonGo = CreateUiObject("StartExpeditionButton", panel.transform);
        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.48f, 0.28f, 1f);
        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(StartExpedition);
        var buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.3f, 0.08f);
        buttonRect.anchorMax = new Vector2(0.7f, 0.22f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;

        var labelGo = CreateUiObject("Label", buttonGo.transform);
        var label = labelGo.AddComponent<Text>();
        label.font = HudFont.GetDefault();
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = result == null ? "출격" : "다시 출격";
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StartExpedition()
    {
        M5SaveService.PrepareForExpedition();
        ExpeditionSession.BeginNewRun();
        SceneManager.LoadScene("InDungeon");
    }
}
