using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ================================================================================================
// [Summary] InventoryPanel
// 화면에 뜨는 인벤토리 UI(2026-09-04 신규, 이후 여러 차례 갱신).
//
// [2026-09-04 개편 1] 사용자 요청: "타르코프 느낌으로 인벤토리랑 장착 아이템을 분리하는
// 느낌으로 할려했지" + "나중에 최대한 덜 손보려면" — 그래서 레이아웃을 전부 수동 좌표 계산
// (anchoredPosition 하드코딩)에서 Unity 레이아웃 그룹(Vertical/HorizontalLayoutGroup +
// ContentSizeFitter/LayoutElement) 기반으로 갈아엎었다. 왼쪽 "장비 칸"(타르코프의 paper-doll
// 자리)과 오른쪽 "가방" 그리드가 완전히 독립된 하위 컴포넌트로 분리되어 있고, 장비 칸은
// VerticalLayoutGroup이 항목 수에 맞춰 세로 길이를 알아서 늘려준다 — 그래서 반지/목걸이
// 슬롯이 생기면 EquipmentSlotType에 항목을 추가하고 아래 EquipmentSlotOrder 배열에 한 줄만
// 더하면 끝이고, 좌표를 손으로 다시 맞출 필요가 전혀 없다.
//
// [2026-09-04 개편 2] 사용자 요청: "이제 빤스칸 만들자 빤스칸은 인벤토리 밑에 2x2를 기본으로
// 만들어주고 기본 외각선을 좀 다르게" — SETTLEMENT_AND_MERCHANT_DESIGN.md의 "빤스(보관함)"
// 개념을 UI로 먼저 만들어둔 것. 가방과 마찬가지로 아직 실제 아이템 데이터는 없는 빈 슬롯
// 뼈대이고(루팅/보관 로직은 범위 밖), 장비 칸/가방과는 별개의 세 번째 세로 섹션으로 패널 맨
// 아래에 붙는다. "외각선을 다르게"는 슬롯 배경 프레임 스프라이트를 가방과 다른 걸 써서
// (FrameThickMedium vs FrameThinSmall) 시각적으로 바로 구분되게 했다.
//
// [2026-09-04 개편 3] GPT 생성 UI 스프라이트 시트(승인됨, Resources/UI_Generated/) 도입 —
// 패널 배경/슬롯 배경을 기존 흰 사각형 틴트 대신 실제 9-slice 프레임 스프라이트로 교체했다.
// GeneratedUiSprites.Get()이 null을 반환하면(아직 임포트 전이거나 에셋이 없으면) 무조건
// 기존 절차적 흰 사각형 폴백으로 자동 전환되므로(ApplyFrame 참고), 에셋 유무와 무관하게 항상
// 안전하게 동작한다 — 새 에셋이 도착하기 전에도 이 코드를 먼저 반영해도 깨지지 않는다.
//
// 아직 아이템 시스템 전체(루팅/드랍)는 범위 밖이라 "가방"/"보관함" 그리드는 여전히 실제 데이터
// 없는 빈 슬롯 뼈대 그대로다.
//
// 장비 슬롯 아이콘 표시 규칙: EquipmentItemDefinition.icon이 있으면 그 스프라이트를, 없으면
// PlayerEquipment.GetFallbackColor()가 주는 색으로 채운 절차적 사각형을 그린다(지금 자동
// 장착되는 테스트용 기본 장비는 전부 icon이 없는 상태 — PlayerEquipment.cs 참고).
//
// 토글 키는 I — Tab은 다른 용도(창 전환 등)로 흔히 예약되는 편이라 충돌을 피했다(Inspector에서
// 바꿀 수 있게 SerializeField로 노출).
//
// [2026-09-04 하이라키 배치 지원] Unity MCP 에디터 연결이 아직 안 잡혀서, 에디터 메뉴 스크립트
// (BuildHudInScene.cs)로 Edit 모드에서 직접 만들어 씬에 저장해두는 방식으로 대신한다.
// [ExecuteAlways]를 붙여서 Edit 모드에서도 Awake가 돌게 하고, Build()는 "이미 씬에 만들어져
// 있으면 새로 만들지 않고 기존 하이라키에서 장비 슬롯 Icon 참조만 다시 찾아 연결"하도록
// 멱등하게 짰다(BindExisting) — 그래야 에디터에서 직접 바꿔둔 슬롯 배경/아이콘 스프라이트가
// Play할 때마다 덮어써지지 않는다. Start()/Update()의 런타임 전용 로직(장비 구독, 토글 입력)은
// Application.isPlaying으로 막아서 에디터에서는 절대 실행되지 않는다.
// ================================================================================================
[ExecuteAlways]
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;
    [SerializeField] private float slotSize = 64f;
    [SerializeField] private float slotSpacing = 8f;

    // [2026-09-04 신규] 빤스칸(보관함) 기본 크기 — 사용자 요청: "인벤토리 밑에 2x2를 기본으로".
    [SerializeField] private int stashColumns = 2;
    [SerializeField] private int stashRows = 2;

    // [2026-09-04] 슬롯 종류만 여기 추가하면 왼쪽 "장비 칸"이 자동으로 한 줄 늘어난다
    // (BuildEquipmentColumn의 VerticalLayoutGroup이 항목 수만큼 알아서 세로 길이를 다시 잡아줌).
    // 반지/목걸이가 생기면 EquipmentSlotType에 항목을 추가하고 여기에 라벨만 추가하면 끝 —
    // 좌표 계산을 손으로 다시 맞출 필요가 없다("나중에 최대한 덜 손보려면"이 이 배열의 존재 이유).
    private static readonly (EquipmentSlotType slot, string label)[] EquipmentSlotOrder =
    {
        (EquipmentSlotType.MainWeapon1, "무기 1"),
        (EquipmentSlotType.MainWeapon2, "무기 2"),
        (EquipmentSlotType.OffHandShield, "방패"),
        (EquipmentSlotType.Armor, "방어구"),
        // [2026-09-04 신규] 반지2/목걸이1 — 사용자 요청("장신구 칸 어딨어 반지 2개 목걸이
        // 한 개 슬롯")으로 슬롯만 미리 추가. VERTICAL_SLICE_SCOPE.md에서 반지/목걸이는
        // "추가 업데이트로 연기"로 명시 확정된 범위 밖 항목이라, 지금은 무기2와 마찬가지로
        // 빈 칸으로만 존재한다(PlayerEquipment가 자동 장착해주는 테스트 아이템 없음) — 실제
        // 반지/목걸이 아이템과 고유 능력은 나중에 그 범위가 열릴 때 추가.
        (EquipmentSlotType.Ring1, "반지 1"),
        (EquipmentSlotType.Ring2, "반지 2"),
        (EquipmentSlotType.Necklace, "목걸이"),
    };

    private GameObject panelRoot;
    private PlayerEquipment subscribedEquipment;
    private readonly Dictionary<EquipmentSlotType, Image> equipmentSlotIcons = new Dictionary<EquipmentSlotType, Image>();

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
        // [ExecuteAlways] 때문에 에디터(Edit 모드)에서도 Start()가 불릴 수 있는데, 장비 구독은
        // 런타임 전용이다 — 에디터에서 실행되면 존재하지도 않는 PlayerEquipment를 찾다가 끝난다.
        if (!Application.isPlaying) return;

        // PlayerEquipment는 PlayerTurnActor.Awake()에서 만들어지는데, 모든 오브젝트의 Awake가
        // Start보다 먼저 끝나는 게 유니티가 보장하는 순서라 여기(Start)서 찾으면 항상 이미
        // 준비되어 있다(DungeonClockDisplay가 DungeonClock을 찾는 것과 동일한 패턴).
        TrySubscribeToEquipment();
        RefreshEquipmentSlots();
    }

    private void OnDestroy()
    {
        if (subscribedEquipment != null)
        {
            subscribedEquipment.OnEquipmentChanged -= RefreshEquipmentSlots;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return; // 에디터에서는 토글 입력 처리 안 함([ExecuteAlways] 대비).

        if (Input.GetKeyDown(toggleKey))
        {
            panelRoot.SetActive(!panelRoot.activeSelf);

            // 아직 구독을 못 했다면(예: Player가 Start 이후에 늦게 생성된 경우) 열 때마다
            // 한 번씩 재시도 — 가벼운 null 체크뿐이라 비용 걱정 없음.
            if (panelRoot.activeSelf && subscribedEquipment == null)
            {
                TrySubscribeToEquipment();
                RefreshEquipmentSlots();
            }
        }
    }

    private void TrySubscribeToEquipment()
    {
        var equipment = PlayerTurnActor.Instance != null ? PlayerTurnActor.Instance.Equipment : null;
        if (equipment == null) return;

        subscribedEquipment = equipment;
        subscribedEquipment.OnEquipmentChanged += RefreshEquipmentSlots;
    }

    // PlayerEquipment.OnEquipmentChanged 콜백 — 장비 슬롯들의 아이콘/색만 다시 그린다.
    private void RefreshEquipmentSlots()
    {
        foreach (var kvp in equipmentSlotIcons)
        {
            var item = subscribedEquipment != null ? subscribedEquipment.GetEquipped(kvp.Key) : null;
            var image = kvp.Value;

            if (item == null)
            {
                image.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
                image.type = Image.Type.Simple;
                image.color = new Color(1f, 1f, 1f, 0.08f); // 빈 슬롯과 같은 톤 — "비어있음"이 바로 보이게.
            }
            else if (item.icon != null)
            {
                image.sprite = item.icon;
                image.type = Image.Type.Simple;
                image.color = Color.white;
            }
            else
            {
                image.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
                image.type = Image.Type.Simple;
                image.color = subscribedEquipment.GetFallbackColor(item);
            }
        }
    }

    private void Build()
    {
        var hudRoot = ScreenSpaceCanvasProvider.GetOrCreateHudRoot();

        // [2026-09-04] 이미 씬에 배치돼 있으면(에디터에서 미리 배치했거나 Play를 여러 번 들어갔다
        // 나온 경우) 새로 만들지 않고 기존 하이라키에서 장비 슬롯 아이콘 참조만 다시 찾아
        // 연결한다 — 사용자가 에디터에서 직접 바꿔둔 슬롯 배경/아이콘 스프라이트가 안 날아간다.
        var existing = hudRoot.Find("InventoryPanel");
        if (existing != null)
        {
            panelRoot = existing.gameObject;
            BindExisting(existing);
            return;
        }

        panelRoot = new GameObject("InventoryPanel");
        panelRoot.transform.SetParent(hudRoot, false);
        var panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        var backgroundImage = panelRoot.AddComponent<Image>();
        ApplyFrame(backgroundImage, GeneratedUiSprites.PanelDarkMedium, new Color(0.05f, 0.05f, 0.08f, 0.9f));

        // 세로: 타이틀 / 본문(장비 칸 + 가방) / 빤스칸(보관함) 순서 — 항목이 늘어나도 손댈 필요
        // 없게 VerticalLayoutGroup + ContentSizeFitter로 패널 전체 크기를 자동 계산한다(수동
        // 좌표/사이즈 계산 전부 제거 — 2026-09-04 개편의 핵심).
        var rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(14, 14, 10, 14);
        rootLayout.spacing = 10f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;

        var rootFitter = panelRoot.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var titleText = CreateLabel(panelRect, "Title", "인벤토리", 20, TextAnchor.MiddleCenter);
        var titleLayoutElement = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayoutElement.preferredHeight = 28f;
        titleLayoutElement.minWidth = columns * slotSize; // 최소한 가방 폭만큼은 타이틀도 넓게.

        // 본문 가로 배치 — 왼쪽 "장비 칸"(타르코프의 paper-doll 자리), 오른쪽 "가방" 그리드.
        // 서로 완전히 독립된 하위 빌더 함수라, 장비 칸에 슬롯이 늘어나도 가방 쪽은 전혀
        // 안 건드려도 된다(사용자 요청: "인벤토리랑 장착 아이템을 분리하는 느낌").
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(panelRect, false);
        var contentRect = contentGo.AddComponent<RectTransform>();

        var contentLayout = contentGo.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;

        var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildEquipmentColumn(contentRect);
        BuildBagGrid(contentRect);

        // 인벤토리(장비 칸 + 가방) 바로 아래, 완전히 별도 섹션으로 빤스칸(보관함)을 붙인다.
        BuildStashSection(panelRect);

        // [2026-09-04 사용자 요청] "그룹으로 묶어놓아서 못 건드리는거구나... 해제 해줘 이거
        // 바인딩 되면 내가 못 건드려" — LayoutGroup/ContentSizeFitter가 붙어있는 동안은 Scene
        // 뷰에서 슬롯/줄을 드래그해도 매 프레임 자동 재계산돼서 계속 원래 자리로 튕겨나간다.
        // 그렇다고 레이아웃 그룹을 처음부터 안 쓰면 슬롯 추가할 때마다 좌표를 손으로 다시 맞춰야
        // 하는 예전 문제로 돌아간다(이번 세션 초반에 그것 때문에 레이아웃 그룹으로 갈아엎었었음).
        // 그래서 절충안: 레이아웃 그룹이 "처음 한 번은" 자동으로 계산하게 놔두고(슬롯 추가/삭제는
        // 여전히 좌표 계산 없이 코드만 고치면 됨), 그 계산이 끝난 직후 결과값을 각 RectTransform에
        // 확정시킨 뒤 레이아웃 그룹 컴포넌트들을 전부 꺼버린다 — 그러면 그 다음부터는 순수
        // RectTransform 값만 남아서 Scene 뷰에서 자유롭게 드래그/리사이즈할 수 있다(꺼진
        // 컴포넌트라 더 이상 자동 재계산을 안 하니까 사용자가 옮긴 값이 그대로 유지된다).
        BakeAndFreezeLayout(panelRect);

        // 평소엔 닫혀있음 — 필요할 때만 토글 키로 연다. (2026-09-04) 원래 Awake()에서 한 번만
        // 호출했는데, RebuildForEditor()로 "기존 컴포넌트는 있지만 자식만 지워진" 경우를 복구할
        // 때도 새로 만든 패널이 기본적으로 닫힌 상태여야 하므로 이쪽(새로 만드는 분기)으로 옮겼다
        // — BindExisting 분기(재연결만 하는 경우)는 사용자가 그때그때 열어둔 상태를 그대로 존중.
        panelRoot.SetActive(false);
    }

    // 레이아웃 그룹들이 계산한 최종 위치/크기를 한 번 강제로 확정시키고, 이후 재계산이 안 일어나게
    // 전부 비활성화한다. Awake() 시점에 한 번만 호출되므로(신규 빌드 경로에서만) 사용자가 이후
    // Scene 뷰에서 자유롭게 옮긴 값을 다음 Build() 호출(=BindExisting 경로)이 건드리지 않는다.
    private void BakeAndFreezeLayout(RectTransform root)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        foreach (var layoutGroup in root.GetComponentsInChildren<LayoutGroup>(true))
        {
            layoutGroup.enabled = false;
        }
        foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true))
        {
            fitter.enabled = false;
        }
    }

    // [2026-09-04 신규] 이미 씬에 만들어져 있는 인벤토리 패널에서 장비 슬롯 Icon 참조만 다시
    // 찾아 연결한다(Build() 상단의 멱등성 체크 참고). 경로는 Build()/BuildEquipmentColumn/
    // BuildEquipmentSlotRow가 실제로 만드는 하이라키 구조와 정확히 일치해야 한다.
    private void BindExisting(Transform panelRootTransform)
    {
        equipmentSlotIcons.Clear();
        foreach (var (slot, _) in EquipmentSlotOrder)
        {
            var iconTransform = panelRootTransform.Find($"Content/EquipmentColumn/EquipSlot_{slot}/Icon");
            if (iconTransform == null)
            {
                Debug.LogWarning($"[InventoryPanel] 기존 하이라키에서 '{slot}' 슬롯 아이콘을 못 찾았습니다 — " +
                                  "슬롯 구성이 바뀌었다면 씬의 InventoryPanel을 지우고 다시 배치해보세요.");
                continue;
            }
            equipmentSlotIcons[slot] = iconTransform.GetComponent<Image>();
        }
    }

    // 왼쪽 "장비 칸" — 슬롯 하나당 한 줄(아이콘 + 라벨). VerticalLayoutGroup이 EquipmentSlotOrder
    // 항목 수만큼 자동으로 세로 길이를 늘려주므로, 반지/목걸이가 추가돼도 이 함수는 그대로다.
    private void BuildEquipmentColumn(Transform parent)
    {
        var columnGo = new GameObject("EquipmentColumn");
        columnGo.transform.SetParent(parent, false);
        columnGo.AddComponent<RectTransform>();

        var columnLayout = columnGo.AddComponent<VerticalLayoutGroup>();
        columnLayout.spacing = 6f;
        columnLayout.childAlignment = TextAnchor.UpperLeft;
        columnLayout.childForceExpandWidth = true;
        columnLayout.childForceExpandHeight = false;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;

        var columnFitter = columnGo.AddComponent<ContentSizeFitter>();
        columnFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        columnFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var (slot, label) in EquipmentSlotOrder)
        {
            BuildEquipmentSlotRow(columnGo.transform, slot, label);
        }
    }

    // 슬롯 한 줄 — [아이콘][라벨] 가로 배치. 라벨 폭을 고정해서 나중에 "목걸이"처럼 더 긴
    // 라벨이 들어와도 아이콘 위치가 안 흔들리게 한다.
    private void BuildEquipmentSlotRow(Transform parent, EquipmentSlotType slot, string label)
    {
        const float iconSize = 48f;
        const float labelWidth = 64f;

        var rowGo = new GameObject($"EquipSlot_{slot}");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<RectTransform>();

        var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(4, 4, 4, 4);
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        // 줄 배경 — 레이아웃 계산에서 제외(ignoreLayout)하고 부모 사각형에 꽉 채워 붙인다.
        // "슬롯이 있다"는 걸 시각적으로 보여주는 용도뿐, 아이콘/라벨 배치엔 관여하지 않는다.
        var backgroundGo = new GameObject("RowBackground");
        backgroundGo.transform.SetParent(rowGo.transform, false);
        var backgroundRect = backgroundGo.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        var backgroundImage = backgroundGo.AddComponent<Image>();
        ApplyFrame(backgroundImage, GeneratedUiSprites.FrameThinSmall, new Color(1f, 1f, 1f, 0.05f));
        var backgroundLayoutElement = backgroundGo.AddComponent<LayoutElement>();
        backgroundLayoutElement.ignoreLayout = true;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(rowGo.transform, false);
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        iconImage.color = new Color(1f, 1f, 1f, 0.08f);
        var iconLayoutElement = iconGo.AddComponent<LayoutElement>();
        iconLayoutElement.preferredWidth = iconSize;
        iconLayoutElement.preferredHeight = iconSize;
        equipmentSlotIcons[slot] = iconImage;

        var labelText = CreateLabel(rowGo.transform, "Label", label, 13, TextAnchor.MiddleLeft);
        labelText.color = new Color(1f, 1f, 1f, 0.75f);
        var labelLayoutElement = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayoutElement.preferredWidth = labelWidth;
        labelLayoutElement.preferredHeight = iconSize;
    }

    // 오른쪽 "가방" 그리드 — 여전히 실제 아이템 데이터 없는 빈 슬롯 뼈대. 장비 칸과 완전히
    // 독립적이라 이쪽 크기(columns x rows)는 장비 슬롯 개수가 늘어나도 전혀 영향받지 않는다.
    private void BuildBagGrid(Transform parent)
    {
        var gridGo = new GameObject("BagGrid");
        gridGo.transform.SetParent(parent, false);
        gridGo.AddComponent<RectTransform>();

        var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(slotSize, slotSize);
        gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        // GridLayoutGroup은 스스로 자기 크기를 계산해주지 않아서(자식 배치만 함), 부모
        // HorizontalLayoutGroup에게 "내가 이 정도 크기를 원한다"고 알려주는 역할로 필요하다.
        var gridLayoutElement = gridGo.AddComponent<LayoutElement>();
        gridLayoutElement.preferredWidth = columns * slotSize + (columns - 1) * slotSpacing;
        gridLayoutElement.preferredHeight = rows * slotSize + (rows - 1) * slotSpacing;

        int totalSlots = columns * rows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateEmptySlot(gridGo.transform, i, GeneratedUiSprites.FrameThinSmall, new Color(1f, 1f, 1f, 0.12f));
        }
    }

    // [2026-09-04 신규] "빤스칸"(보관함) 섹션 — 인벤토리(장비 칸+가방) 바로 아래, 완전히 별도의
    // 세로 섹션. 사용자 요청: "빤스칸은 인벤토리 밑에 2x2를 기본으로 만들어주고 기본 외각선을
    // 좀 다르게" — 그래서 슬롯 배경을 가방(FrameThinSmall)과 다른 프레임(FrameThickMedium)으로
    // 써서 "여긴 다른 종류의 보관함"이라는 게 한눈에 구분되게 했다. 실제 보관함 데이터/용량
    // 확장(SETTLEMENT_AND_MERCHANT_DESIGN.md의 "보관함 확장 퀘스트")은 범위 밖 — 지금은 가방과
    // 동일하게 빈 슬롯 뼈대만.
    private void BuildStashSection(Transform parent)
    {
        var sectionGo = new GameObject("StashSection");
        sectionGo.transform.SetParent(parent, false);
        sectionGo.AddComponent<RectTransform>();

        var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 4f;
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childForceExpandWidth = false;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;

        var sectionFitter = sectionGo.AddComponent<ContentSizeFitter>();
        sectionFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var stashTitle = CreateLabel(sectionGo.transform, "StashTitle", "보관함", 14, TextAnchor.MiddleCenter);
        stashTitle.color = new Color(1f, 0.85f, 0.6f, 0.9f); // 가방과 다른 색조로 살짝 구분.
        var stashTitleLayout = stashTitle.gameObject.AddComponent<LayoutElement>();
        stashTitleLayout.preferredHeight = 20f;

        var gridGo = new GameObject("StashGrid");
        gridGo.transform.SetParent(sectionGo.transform, false);
        gridGo.AddComponent<RectTransform>();

        var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(slotSize, slotSize);
        gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = stashColumns;

        var gridLayoutElement = gridGo.AddComponent<LayoutElement>();
        gridLayoutElement.preferredWidth = stashColumns * slotSize + (stashColumns - 1) * slotSpacing;
        gridLayoutElement.preferredHeight = stashRows * slotSize + (stashRows - 1) * slotSpacing;

        int totalSlots = stashColumns * stashRows;
        for (int i = 0; i < totalSlots; i++)
        {
            CreateEmptySlot(gridGo.transform, i, GeneratedUiSprites.FrameThickMedium, new Color(0.9f, 0.75f, 0.3f, 0.18f), "StashSlot_");
        }
    }

    // 빈 슬롯 하나 — 지금은 배경 프레임뿐이라 시각적으로 "칸이 있다"만 보여준다. 아이템이
    // 들어있는지 여부는 3단계 후반(루팅/드랍)에서 이 자리에 아이콘 Image를 추가/활성화하는
    // 식으로 확장하면 된다 — 지금 만든 슬롯 개수/레이아웃 자체는 그대로 유지 가능.
    // frameSpriteName/fallbackColor를 인자로 받아서 가방/빤스칸이 서로 다른 프레임을 쓸 수
    // 있게 했다(사용자 요청: 빤스칸 외곽선을 가방과 다르게).
    private void CreateEmptySlot(Transform parent, int index, string frameSpriteName, Color fallbackColor, string namePrefix = "Slot_")
    {
        var slotGo = new GameObject($"{namePrefix}{index}");
        slotGo.transform.SetParent(parent, false);

        var slotImage = slotGo.AddComponent<Image>();
        ApplyFrame(slotImage, frameSpriteName, fallbackColor);
    }

    // GPT 생성 UI 스프라이트(9-slice 프레임)가 있으면 그걸 Sliced 모드로 쓰고, 없으면
    // (아직 임포트 전/에셋 없음) 기존 절차적 흰 사각형 + 색상 틴트로 자동 폴백한다 — 이
    // 클래스를 쓰는 모든 배경/슬롯이 에셋 유무와 무관하게 항상 안전하게 그려진다.
    private void ApplyFrame(Image image, string spriteName, Color fallbackColor)
    {
        var sprite = GeneratedUiSprites.Get(spriteName);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white; // 원본 아트 색을 그대로 살린다 — 틴트 없음.
        }
        else
        {
            image.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
            image.type = Image.Type.Simple;
            image.color = fallbackColor;
        }
    }

    private Text CreateLabel(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = HudFont.GetDefault();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }
}
