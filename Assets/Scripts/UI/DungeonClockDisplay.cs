using UnityEngine;
using UnityEngine.UI;

// ================================================================================================
// [Summary] DungeonClockDisplay
// 화면 왼쪽 위에 던전 실시계를 아날로그(시침+분침) 형태로 띄운다(2026-09-04, 사용자 피드백:
// "아날로그 시계형태말고 분침 시침으로만 이루어진 시계로 표현하려했는데" — 처음엔 "HH:mm" 숫자
// 텍스트로 만들었었는데, 원하는 건 숫자가 아니라 실제로 바늘이 도는 시계판이었다는 걸 확인).
//
// 시간 계산 로직은 여기 전혀 없다(DungeonClock.cs가 유일한 소스) — 이미 있는 GetClockString()
// ("HH:mm")을 파싱해서 바늘 각도만 계산하는 순수 뷰다(단일 책임 유지).
//
// 바늘 구현 방식: RectTransform pivot을 바닥 중앙(0.5, 0)으로 두고 시계 중심에 배치한 뒤,
// sizeDelta로 바늘 길이/두께를 정하고 Z축 회전만 준다 — 12시 방향(위쪽)이 회전각 0, 시계
// 방향(오른쪽으로 도는 방향)으로 갈수록 각도가 커지므로 Unity UI의 반시계 방향 양(+)회전
// 관례와 반대라 항상 각도에 마이너스를 붙여서 적용한다(아래 UpdateHands 주석 참고).
//
// AlertIndicator/HP바와 동일한 "완전 자체 생성" 패턴 — 씬에 아무 설정 없이 이 컴포넌트만 붙이면
// (DungeonSceneBootstrapper가 자동으로 붙여준다) Canvas부터 시계판/바늘까지 전부 코드로 만든다.
//
// [2026-09-04 하이라키 배치 지원] 사용자가 "런타임 생성되는 자리에 그대로 배치"해달라고 요청 —
// Unity MCP 에디터 연결이 아직 안 잡혀서, 에디터 메뉴 스크립트(BuildHudInScene.cs)로 이 컴포넌트를
// Edit 모드에서 직접 만들어 씬에 저장해두는 방식으로 대신한다. [ExecuteAlways]를 붙여서 Edit
// 모드에서도 Awake가 돌게 하고, Build()는 "이미 씬에 만들어져 있으면 새로 만들지 않고 기존
// 하이라키에서 참조만 다시 찾아 연결"하도록 멱등하게 짰다(BindExisting) — 그래야 (1) 에디터에서
// 한 번 배치해둔 뒤 사용자가 직접 스프라이트/폰트를 바꿔놔도 Play할 때마다 새로 덮어써지지 않고,
// (2) 스크립트 재컴파일로 Awake가 다시 불려도 하이라키에 중복 생성되지 않는다. Start()/Update()의
// 런타임 전용 로직(DungeonClock 구독 등)은 Application.isPlaying으로 막아서 에디터에서는 절대
// 실행되지 않는다.
// ================================================================================================
[ExecuteAlways]
public class DungeonClockDisplay : MonoBehaviour
{
    [SerializeField] private Vector2 anchoredPosition = new Vector2(24f, -24f);
    [SerializeField] private float faceDiameter = 96f;
    [SerializeField] private int remainingTextFontSize = 16;

    private DungeonClock dungeonClock;
    private RectTransform hourHandRect;
    private RectTransform minuteHandRect;
    private Text remainingText;

    private void Awake()
    {
        Build();
    }

    // [2026-09-04 신규] 에디터 메뉴(BuildHudInScene)가 "컴포넌트는 이미 붙어있는데(Awake가 다시
    // 안 불림) 만들어뒀던 하이라키 자식만 지워진" 경우를 복구할 수 있게 Build()를 외부에서
    // 강제로 다시 부를 수 있게 노출한다. Build() 자체가 멱등하므로(있으면 재연결, 없으면 새로
    // 생성) 몇 번을 불러도 안전하다.
    public void RebuildForEditor()
    {
        Build();
    }

    private void Start()
    {
        // [ExecuteAlways] 때문에 에디터(Edit 모드)에서도 Start()가 불릴 수 있는데, DungeonClock
        // 구독 같은 런타임 전용 로직은 Play 중에만 의미가 있다 — 에디터에서 실행되면 존재하지도
        // 않는 DungeonClock을 찾다가 매번 경고만 찍고 끝난다.
        if (!Application.isPlaying) return;

        // DungeonClock은 TickManager와 같은 오브젝트에 붙는 구조라(DungeonSceneBootstrapper.cs
        // 참고) 씬에 하나만 있다는 전제 — FindObjectOfType으로 충분하다. Awake가 아니라 Start에서
        // 찾는 이유: DungeonSceneBootstrapper.Start()가 DungeonClock을 그 시점에 만들 수도 있어서
        // (TickManager.Instance가 없던 경우), 모든 Awake가 끝난 뒤인 Start 시점이 더 안전하다.
        dungeonClock = FindObjectOfType<DungeonClock>();
        if (dungeonClock == null)
        {
            Debug.LogWarning("[DungeonClockDisplay] 씬에서 DungeonClock을 못 찾았습니다 — 시계가 갱신되지 않습니다.");
            return;
        }

        dungeonClock.OnClockChanged += HandleClockChanged;
        RefreshDisplay(); // 최초 1회 즉시 반영 — 첫 행동 전까지 바늘이 엉뚱한 위치에 멈춰있지 않도록.
    }

    private void OnDestroy()
    {
        if (dungeonClock != null)
        {
            dungeonClock.OnClockChanged -= HandleClockChanged;
        }
    }

    private void HandleClockChanged(float elapsedTicks)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (dungeonClock == null) return;

        // GetClockString()이 이미 "HH:mm" 포맷을 만들어주니, 여기서 새로 시/분을 계산하지 않고
        // 그 문자열을 그대로 파싱만 한다 — 시간 계산 책임은 계속 DungeonClock 하나에만 있게 유지.
        var parts = dungeonClock.GetClockString().Split(':');
        int hour = int.Parse(parts[0]);
        int minute = int.Parse(parts[1]);
        UpdateHands(hour, minute);

        float remainingMinutes = dungeonClock.RemainingTicks * DungeonClock.MinutesPerTick;
        int remainingHours = Mathf.FloorToInt(remainingMinutes / 60f);
        int remainingMins = Mathf.FloorToInt(remainingMinutes % 60f);
        remainingText.text = $"복귀까지 {remainingHours}h {remainingMins}m";
    }

    // 12시 방향(위쪽)을 0도로 두고, 시계가 도는 방향(오른쪽 → 아래 → 왼쪽)으로 각도가 커진다.
    // Unity UI의 Z축 회전은 반대로 반시계 방향이 양수라서, 실제로 적용할 때는 항상 부호를
    // 뒤집어서(-angle) 넣어야 화면에서 시계 방향으로 정확히 돈다.
    private void UpdateHands(int hour, int minute)
    {
        float minuteFraction = minute / 60f;
        float minuteAngle = minuteFraction * 360f;
        float hourAngle = ((hour % 12) + minuteFraction) / 12f * 360f;

        hourHandRect.localEulerAngles = new Vector3(0f, 0f, -hourAngle);
        minuteHandRect.localEulerAngles = new Vector3(0f, 0f, -minuteAngle);
    }

    private void Build()
    {
        var hudRoot = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();

        // [2026-09-04] 이미 씬에 배치돼 있으면(에디터에서 미리 배치했거나, Play를 여러 번 들어갔다
        // 나온 경우) 새로 만들지 않고 기존 하이라키에서 참조만 다시 찾아 연결한다 — 그래야 사용자가
        // 에디터에서 직접 바꿔둔 스프라이트/폰트가 덮어써지지 않는다.
        var existing = hudRoot.Find("DungeonClockDisplay");
        if (existing != null)
        {
            BindExisting(existing);
            return;
        }

        var panelGo = new GameObject("DungeonClockDisplay");
        panelGo.transform.SetParent(hudRoot, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = new Vector2(faceDiameter, faceDiameter + 28f); // 시계판 + 아래 남은시간 텍스트 여백.

        // 시계판(원) — 살짝 어두운 배경 위에 흰 테두리를 얹는 2겹 구조로 "판 + 테두리" 느낌을 낸다.
        var faceCenter = new Vector2(faceDiameter * 0.5f, -faceDiameter * 0.5f);

        var borderGo = new GameObject("Face_Border");
        borderGo.transform.SetParent(panelRect, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0f, 1f);
        borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.anchoredPosition = faceCenter;
        borderRect.sizeDelta = new Vector2(faceDiameter, faceDiameter);
        var borderImage = borderGo.AddComponent<Image>();
        borderImage.sprite = ProceduralSprite.GetCircleSprite();
        borderImage.color = new Color(1f, 1f, 1f, 0.85f);

        float faceInset = 4f; // 테두리 두께.
        var faceGo = new GameObject("Face_Background");
        faceGo.transform.SetParent(borderRect, false);
        var faceRect = faceGo.AddComponent<RectTransform>();
        faceRect.anchorMin = new Vector2(0.5f, 0.5f);
        faceRect.anchorMax = new Vector2(0.5f, 0.5f);
        faceRect.pivot = new Vector2(0.5f, 0.5f);
        faceRect.anchoredPosition = Vector2.zero;
        faceRect.sizeDelta = new Vector2(faceDiameter - faceInset * 2f, faceDiameter - faceInset * 2f);
        var faceImage = faceGo.AddComponent<Image>();
        faceImage.sprite = ProceduralSprite.GetCircleSprite();
        faceImage.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        // 12/3/6/9시 위치 눈금 — 없으면 원판이 "지금 몇 시쯤인지" 가늠할 기준점이 아예 없어서
        // 바늘 각도만으로는 읽기 어렵다. 작은 흰 점 4개만 최소한으로 찍는다.
        float tickInset = faceDiameter * 0.5f - faceInset - 6f;
        CreateTickMark(faceRect, new Vector2(0f, tickInset));   // 12시
        CreateTickMark(faceRect, new Vector2(tickInset, 0f));   // 3시
        CreateTickMark(faceRect, new Vector2(0f, -tickInset));  // 6시
        CreateTickMark(faceRect, new Vector2(-tickInset, 0f));  // 9시

        // 시침 — 짧고 굵게. (2026-09-04) 두 바늘 다 "Hand"라는 같은 이름이면 나중에 씬에 미리
        // 배치해둔 걸 재바인딩할 때(BindExisting 참고) transform.Find로 구분이 안 되므로
        // "HourHand"/"MinuteHand"로 고유하게 이름 붙인다.
        hourHandRect = CreateHand(faceRect, faceDiameter * 0.28f, 5f, new Color(1f, 1f, 1f, 0.95f), "HourHand");
        // 분침 — 길고 얇게. 나중에 그려서 시침 위에 겹치게 한다(분침이 더 눈에 띄어야 함).
        minuteHandRect = CreateHand(faceRect, faceDiameter * 0.42f, 3f, new Color(0.75f, 0.9f, 1f, 0.95f), "MinuteHand");

        // 중심 축 — 바늘 두 개가 만나는 지점을 작은 원으로 덮어서 이음매를 가려준다.
        var pivotDotGo = new GameObject("Face_PivotDot");
        pivotDotGo.transform.SetParent(faceRect, false);
        var pivotDotRect = pivotDotGo.AddComponent<RectTransform>();
        pivotDotRect.anchorMin = new Vector2(0.5f, 0.5f);
        pivotDotRect.anchorMax = new Vector2(0.5f, 0.5f);
        pivotDotRect.pivot = new Vector2(0.5f, 0.5f);
        pivotDotRect.anchoredPosition = Vector2.zero;
        pivotDotRect.sizeDelta = new Vector2(8f, 8f);
        var pivotDotImage = pivotDotGo.AddComponent<Image>();
        pivotDotImage.sprite = ProceduralSprite.GetCircleSprite();
        pivotDotImage.color = Color.white;

        // 남은 시간 텍스트 — 시계판 바로 아래, 작게.
        var remainingGo = new GameObject("RemainingText");
        remainingGo.transform.SetParent(panelRect, false);
        var remainingRect = remainingGo.AddComponent<RectTransform>();
        remainingRect.anchorMin = new Vector2(0f, 1f);
        remainingRect.anchorMax = new Vector2(0f, 1f);
        remainingRect.pivot = new Vector2(0f, 1f);
        remainingRect.anchoredPosition = new Vector2(0f, -(faceDiameter + 4f));
        remainingRect.sizeDelta = new Vector2(faceDiameter + 60f, remainingTextFontSize + 6f);
        remainingText = remainingGo.AddComponent<Text>();
        remainingText.font = HudFont.GetDefault();
        remainingText.fontSize = remainingTextFontSize;
        remainingText.alignment = TextAnchor.UpperLeft;
        remainingText.color = new Color(1f, 1f, 1f, 0.8f);

        var shadow = remainingGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    // [2026-09-04 신규] 이미 씬에 만들어져 있는 시계판에서 바늘/텍스트 참조만 다시 찾아 연결한다
    // (Build() 상단의 멱등성 체크 참고). 경로는 Build()가 실제로 만드는 하이라키 구조와 정확히
    // 일치해야 한다 — 구조가 바뀌면 이 경로도 같이 고쳐야 함.
    private void BindExisting(Transform panelRoot)
    {
        hourHandRect = panelRoot.Find("Face_Border/Face_Background/HourHand") as RectTransform;
        minuteHandRect = panelRoot.Find("Face_Border/Face_Background/MinuteHand") as RectTransform;
        var remainingTransform = panelRoot.Find("RemainingText");
        remainingText = remainingTransform != null ? remainingTransform.GetComponent<Text>() : null;

        if (hourHandRect == null || minuteHandRect == null || remainingText == null)
        {
            Debug.LogWarning("[DungeonClockDisplay] 기존 하이라키에서 일부 참조를 못 찾았습니다 — " +
                              "씬에 있는 DungeonClockDisplay 오브젝트를 지우고 다시 배치해보세요.");
        }
    }

    // 문자판 눈금 하나 — localPosition이 이미 "중심 기준 오프셋"이라 그대로 anchoredPosition으로 쓴다.
    private void CreateTickMark(RectTransform parent, Vector2 localOffset)
    {
        var go = new GameObject("TickMark");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localOffset;
        rect.sizeDelta = new Vector2(4f, 4f);
        var image = go.AddComponent<Image>();
        image.sprite = ProceduralSprite.GetCircleSprite();
        image.color = new Color(1f, 1f, 1f, 0.7f);
    }

    // 바늘 하나 — pivot을 바닥 중앙(0.5, 0)에 둬서 시계 중심에 고정하고, 위쪽(12시 방향)으로
    // length만큼 뻗은 사각형을 만든다. 실제 회전은 UpdateHands()에서 매번 다시 설정한다.
    private RectTransform CreateHand(RectTransform parent, float length, float thickness, Color color, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f); // 바닥 중앙이 시계 중심 = 회전축.
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(thickness, length);

        var image = go.AddComponent<Image>();
        image.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        image.color = color;

        return rect;
    }
}
