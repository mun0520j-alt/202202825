using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 9) 실제 플레이어의 턴 참여 + 이동 담당. TickManager 큐에서 자기 차례
// (OnTurnStart)가 오면 입력을 기다렸다가, 유효한 이동이 들어오면 그만큼 tick을 소비한다.
//
// 이동 방식 두 가지(2026-08-27 설계 확정):
//   1) 방향키(WASD/화살표) — 한 칸씩, 4방향만(대각선 없음)
//   2) 마우스 클릭 — 녹픽던처럼 클릭한 타일까지 경로를 찾아서 한 칸씩 자동으로 걸어감.
//      자동으로 걷는 동안에도 매 칸 이동은 방향키 이동과 완전히 같은 경로(TryStep)를 타므로
//      tick 소비량도 동일하게 계산된다 — "자동 이동이라 tick을 따로 관리"하지 않는다.
//      몹을 발견하면(지금은 몹이 없어서 항상 false인 스텁) 자동 이동을 즉시 멈춘다.
//
// 이동 연출: 논리 좌표(currentCell)는 이동이 확정되는 즉시 갱신하고, 시각적인 트랜스폼
// 이동만 코루틴으로 짧게(hopDuration) 살짝 점프하는 곡선을 그리며 보간한다(네크로댄서 참고).
// 이 연출 시간은 실시간(Time.deltaTime) 기준이라 tick(턴) 시스템과는 완전히 무관하다.
[RequireComponent(typeof(Transform))]
public class PlayerTurnActor : MonoBehaviour, ITurnActor
{
    [Header("타일맵 참조 (Inspector에서 MapGenPreview/Floor, /Walls 드래그해서 연결)")]
    [Tooltip("벽 충돌 판정용. 이 칸에 타일이 있으면 이동 불가.")]
    [SerializeField] private Tilemap wallsTilemap;
    [Tooltip("바닥 판정용 + 셀<->월드 좌표 변환 기준. 이 칸에 타일이 없으면(맵 바깥) 이동 불가.")]
    [SerializeField] private Tilemap floorTilemap;

    [Header("이동 연출 (실시간 기준 — tick과 무관)")]
    [Tooltip("한 칸 이동에 걸리는 시각 연출 시간(초).")]
    [SerializeField] private float hopDuration = 0.12f;
    [Tooltip("점프 최고점 높이(월드 유닛).")]
    [SerializeField] private float hopHeight = 0.15f;

    [Header("시야 (Fog of War, 2026-08-28 추가)")]
    [Tooltip("Player가 실제로 볼 수 있는 최대 거리(칸, 원형 반경). FogOfWarController가 이 값으로 안개를 걷는다.")]
    [SerializeField] private int sightRangeInTiles = 6;

    // 다른 컴포넌트(EnemyTurnActor 등)가 "지금 Player가 어디 있는지"를 조회할 수 있게 하는
    // 싱글턴 — TickManager.Instance와 같은 패턴(2026-08-28 추가). Player는 씬에 항상 하나만
    // 존재한다는 전제가 이미 있어서(OnEnable 주석 참고) 싱글턴으로 노출해도 안전하다.
    public static PlayerTurnActor Instance { get; private set; }

    // 플레이어는 입력을 기다리며 오래 있는 게 정상 동작이라 TickManager의 "CompleteTurn
    // 호출 누락 감지 워치독" 대상에서 제외한다(설계 확정 사항, TickManager.cs 참고).
    public bool SuppressStuckTurnWarning => true;

    // ITurnActor.CurrentCell 구현 — TickManager.IsCellOccupied()가 Player/Enemy 겹침 방지를 위해
    // 조회한다(2026-08-28 추가, ITurnActor.cs 주석 참고).
    public Vector3Int CurrentCell => currentCell;

    // 방향키 -> 이동 방향(셀 단위) 매핑. 4방향만(대각선 없음, 2026-08-27 확정).
    private static readonly Dictionary<KeyCode, Vector3Int> DirectionKeys = new Dictionary<KeyCode, Vector3Int>
    {
        { KeyCode.W, new Vector3Int(0, 1, 0) },
        { KeyCode.UpArrow, new Vector3Int(0, 1, 0) },
        { KeyCode.S, new Vector3Int(0, -1, 0) },
        { KeyCode.DownArrow, new Vector3Int(0, -1, 0) },
        { KeyCode.A, new Vector3Int(-1, 0, 0) },
        { KeyCode.LeftArrow, new Vector3Int(-1, 0, 0) },
        { KeyCode.D, new Vector3Int(1, 0, 0) },
        { KeyCode.RightArrow, new Vector3Int(1, 0, 0) },
    };

    // 좌우 이동 시 스프라이트를 뒤집어서 바라보는 방향을 표현한다(2026-08-27 추가).
    // GetComponentInChildren를 쓰는 이유: knight_m 같은 프리팹은 SpriteRenderer가 이
    // 오브젝트 자신이 아니라 자식 오브젝트에 붙어있을 수도 있어서(둘 다 대응).
    private SpriteRenderer spriteRenderer;

    private Vector3Int currentCell;
    private bool isMyTurn;
    private bool isAnimating;

    // 마우스 클릭으로 예약된 자동 이동 경로. 비어있으면 자동 이동 없음.
    private Queue<Vector3Int> queuedAutoPath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerTurnActor] 씬에 이미 Player가 있어서 중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (wallsTilemap == null || floorTilemap == null)
        {
            Debug.LogError("[PlayerTurnActor] wallsTilemap/floorTilemap이 Inspector에 연결 안 돼있습니다 — " +
                            "MapGenPreview 하위의 Walls/Floor 타일맵을 드래그해서 연결해주세요.");
            enabled = false;
            return;
        }

        // 시작 위치를 논리 좌표로 스냅 — 에디터 툴이 배치한 월드 좌표를 셀 좌표로 역산.
        currentCell = floorTilemap.WorldToCell(transform.position);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[PlayerTurnActor] SpriteRenderer를 못 찾아서 좌우 반전 연출은 생략됩니다.");
        }

    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Awake가 아니라 Start에서 첫 안개 갱신을 호출한다 — Unity는 씬 안의 모든 컴포넌트 Awake가
    // 끝난 뒤에야 Start를 부르는 게 보장돼 있어서(반대로 Awake끼리는 순서 보장이 없음),
    // FogOfWarController.Instance가 아직 null인 상태로 이 호출이 씹히는 걸 막을 수 있다.
    // 이게 없으면 첫 이동 전까지 화면 전체가 안개로 덮여있게 된다(FogOfWarController.Awake()가
    // 기본값을 "전부 안개"로 시작해서).
    private void Start()
    {
        FogOfWarController.Instance?.UpdateVisibility(currentCell, sightRangeInTiles);
    }

    // 씬에 이미 배치되어 있는 실제 플레이어라서(스포너가 따로 없음), 자기 자신이 활성화될 때
    // 스스로 TickManager에 등록/해제한다 — TickQueueTestActor처럼 외부 부트스트래퍼가 등록해주는
    // 방식과 다른 이유는 "플레이어는 항상 씬에 존재한다"는 전제가 있기 때문(설계 확정 사항).
    private void OnEnable()
    {
        var tickManager = TickManager.Instance;
        if (tickManager == null)
        {
            // 초기화 순서상 TickManager가 아직 씬에 없을 수도 있어서(다른 오브젝트의 Awake가
            // 아직 안 돌았을 경우), 없으면 여기서 만들어둔다. 이후 DungeonSceneBootstrapper의
            // Start()가 이미 존재하는 TickManager.Instance를 그대로 재사용해서 BeginSchedule()만
            // 호출하게 된다.
            var host = new GameObject("TickManager(AutoCreated)");
            tickManager = host.AddComponent<TickManager>();
        }
        tickManager.RegisterActor(this);
    }

    private void OnDisable()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.UnregisterActor(this);
        }
    }

    public void OnTurnStart()
    {
        isMyTurn = true;
        // 실제 입력 처리는 Update()에서 매 프레임 폴링한다 — 여기서는 "내 턴 시작됨" 표시만.
    }

    private void Update()
    {
        if (!isMyTurn || isAnimating) return;

        // 수동 입력(방향키/클릭)은 항상 자동 이동보다 우선한다 — 자동 이동 중에 방향키를
        // 누르거나 다른 타일을 클릭하면 기존 예약 경로를 버리고 새 입력을 따른다.
        if (TryReadDirectionKey(out var manualStep))
        {
            ClearQueuedAutoPath();
            TryStep(manualStep);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
            return; // 클릭 처리 자체는 경로 예약까지만, 실제 첫 걸음은 다음 줄부터 이어서 처리
        }

        if (queuedAutoPath != null && queuedAutoPath.Count > 0)
        {
            StepAlongAutoPath();
        }
    }

    private bool TryReadDirectionKey(out Vector3Int step)
    {
        foreach (var kvp in DirectionKeys)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                step = kvp.Value;
                return true;
            }
        }
        step = default;
        return false;
    }

    private void HandleMouseClick()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        worldPoint.z = 0f;
        var clickedCell = floorTilemap.WorldToCell(worldPoint);

        // [임시 디버그 로그 — 2026-08-28] 특정 모서리 칸이 왜 막혀있는지 확인하려고 좌표 확인용으로
        // 잠깐 추가함. 원인 확인 끝나면 지워도 되는 라인.
        Debug.Log($"[DEBUG 좌표확인] 클릭한 셀 {clickedCell} — floor={floorTilemap.HasTile(clickedCell)}, " +
                  $"wall={wallsTilemap.HasTile(clickedCell)}");

        // 클릭한 칸 자체가 바닥이 아니면(벽이거나 맵 바깥) 그냥 무시 — 아무 반응 없음.
        if (!floorTilemap.HasTile(clickedCell) || wallsTilemap.HasTile(clickedCell)) return;

        var path = TilePathfinder.FindPath(currentCell, clickedCell, wallsTilemap, floorTilemap);
        if (path.Count == 0) return; // 갈 수 없는 칸(도달 불가) — 무시

        queuedAutoPath = new Queue<Vector3Int>(path);
        StepAlongAutoPath(); // 클릭한 프레임에 바로 첫 걸음을 내딛어서 반응성을 높인다.
    }

    // 예약된 경로를 한 칸 진행한다. 몹을 발견하면(지금은 항상 false — 몹 시스템 붙기 전까지의
    // 스텁) 남은 경로를 버리고 자동 이동을 멈춘다 — 이 시점엔 턴이 안 넘어가고 플레이어
    // 입력 대기 상태로 조용히 돌아간다(정상적인 "내 턴" 상태와 동일).
    private void StepAlongAutoPath()
    {
        if (IsMonsterVisibleNow())
        {
            ClearQueuedAutoPath();
            return;
        }

        var nextCell = queuedAutoPath.Dequeue();
        var step = nextCell - currentCell;
        bool moved = TryStep(step);

        if (!moved)
        {
            // 정상적으로는 경로 탐색 시점에 이미 벽이 아닌 칸들만 골랐으니 여기 걸릴 일은
            // 없어야 하지만, 혹시 경로 계산 이후 맵이 바뀌는 등의 예외 상황을 대비해 방어.
            ClearQueuedAutoPath();
        }
    }

    // 몹 시야 감지 시스템이 아직 없어서 항상 false를 반환하는 스텁이다. 나중에 Enemy
    // 가시성 체크(FOV, 감지 범위 등)가 생기면 이 메서드 내부만 교체하면 된다 —
    // PlayerTurnActor의 나머지 로직은 안 건드려도 됨(단일 책임 원칙).
    private bool IsMonsterVisibleNow()
    {
        return false;
    }

    private void ClearQueuedAutoPath()
    {
        queuedAutoPath?.Clear();
        queuedAutoPath = null;
    }

    // 셀의 목표 월드 좌표를 계산한다. X는 칸 중앙(정수 + 0.5), Y는 칸의 바닥 경계(정수)로
    // 일부러 다르게 잡는다(2026-08-27 피드백) — Tilemap.GetCellCenterWorld()는 X/Y를 둘 다
    // 중앙(+0.5)으로 주는데, Y까지 .5가 되면 캐릭터의 발밑 Collider가 타일 경계와 안 맞아서
    // 아래/위 타일과 어긋나게 겹치는 문제가 생긴다. 그래서 X만 중앙에 맞추고 Y는 Tilemap의
    // 셀 원점(바닥 경계, CellToWorld가 주는 값)을 그대로 써서 정수로 남긴다.
    private Vector3 GetCellFootWorldPosition(Vector3Int cell)
    {
        Vector3 cellOrigin = floorTilemap.CellToWorld(cell); // 셀의 바닥-왼쪽 모서리(정수 좌표)
        Vector3 cellSize = floorTilemap.cellSize;
        return cellOrigin + new Vector3(cellSize.x * 0.5f, 0f, 0f);
    }

    // 한 칸 이동을 시도한다. 벽이면 false(턴 소모 없음), 성공하면 애니메이션 코루틴을 시작하고 true.
    private bool TryStep(Vector3Int step)
    {
        var targetCell = currentCell + step;

        if (wallsTilemap.HasTile(targetCell))
        {
            // [임시 디버그 로그 — 2026-08-28] "목적지는 뚫려있는데 경로 중간에서 멈춘다" 증상
            // 원인 추적용. StepAlongAutoPath()가 실패 이유를 안 찍고 조용히 자동 이동만 취소해서
            // 셋 중 어느 이유로 막혔는지 안 보였음 — 확인 끝나면 이 세 로그는 지워도 됨.
            Debug.Log($"[DEBUG 이동실패] {targetCell} — 벽에 막힘");
            return false;
        }
        if (!floorTilemap.HasTile(targetCell)) // 맵 바깥으로는 못 나감
        {
            Debug.Log($"[DEBUG 이동실패] {targetCell} — 바닥 타일 없음(맵 바깥)");
            return false;
        }
        // 벽/바닥과 같은 자격으로 "이미 다른 액터가 서 있는 칸"도 막는다 — 콜라이더가 아니라
        // TickManager에 직접 물어보는 이유는 이동을 물리엔진 없이 transform.position으로 직접
        // 처리하는 구조라서다(2026-08-28 발견, ITurnActor.cs 주석 참고).
        if (TickManager.Instance.IsCellOccupied(targetCell, this))
        {
            Debug.Log($"[DEBUG 이동실패] {targetCell} — 다른 액터가 점유 중(IsCellOccupied)");
            return false;
        }

        UpdateFacingDirection(step);
        StartCoroutine(HopTo(targetCell));
        return true;
    }

    // 좌우로 움직일 때만 스프라이트를 반전한다 — 위/아래로만 움직일 때는 직전에 보던
    // 좌우 방향을 그대로 유지한다(흔한 2D 캐릭터 관례: 세로 이동으로 좌우 방향 정보가
    // 없어지지 않게 함).
    private void UpdateFacingDirection(Vector3Int step)
    {
        if (spriteRenderer == null || step.x == 0) return;
        spriteRenderer.flipX = step.x < 0; // 왼쪽으로 가면 반전, 오른쪽이 기본 방향이라고 가정.
    }

    private IEnumerator HopTo(Vector3Int targetCell)
    {
        isAnimating = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = GetCellFootWorldPosition(targetCell);

        float t = 0f;
        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / hopDuration);
            // 수평 이동은 선형 보간, 수직으로는 사인 곡선을 얹어서 살짝 뛰어오르는 느낌을 낸다.
            Vector3 flatPos = Vector3.Lerp(startPos, endPos, p);
            float hopOffset = Mathf.Sin(p * Mathf.PI) * hopHeight;
            transform.position = flatPos + new Vector3(0f, hopOffset, 0f);
            yield return null;
        }

        transform.position = endPos; // 부동소수점 오차 정리 — 정확히 셀 중앙에 스냅.
        currentCell = targetCell; // 논리 좌표는 애니메이션이 끝나는 시점에 확정한다.
        isAnimating = false;

        // 이동할 때마다 안개를 다시 계산한다 — Enemy의 Trace 감지와 같은 LineOfSight를 써서
        // "실제로 지금 보이는 곳"만 걷어낸다(2026-08-28 추가).
        FogOfWarController.Instance?.UpdateVisibility(currentCell, sightRangeInTiles);

        // isMyTurn=false를 CompleteTurn()보다 먼저 세팅한다 — 순서가 중요하다(2026-08-27
        // 버그 발견/수정). 등록된 액터가 플레이어 혼자뿐인 상황에서는 CompleteTurn() 호출 안에서
        // TickManager가 곧바로 다음 차례를 찾는데(AdvanceSchedule), 다음 차례도 결국 플레이어
        // 자신이라 OnTurnStart()가 CompleteTurn() 호출이 "끝나기 전에" 동기적으로 다시 불려서
        // isMyTurn을 true로 세팅해버린다. 그 상태에서 이 아래 줄이 isMyTurn=false를 나중에
        // 실행하면 방금 시작된 새 턴의 플래그를 덮어써서 Update()가 영원히 입력을 무시하게 된다
        // (실제로 이 버그로 "한 번 움직이면 멈추는" 증상이 발생했었음). 그래서 반드시
        // CompleteTurn() 호출보다 먼저 false로 내려서, 그 안에서 새 턴이 시작되며 다시
        // true로 세팅되더라도 최종적으로 true가 남게 만든다.
        isMyTurn = false;

        // 이동 1칸 = TickCost.PerTileMove(0.2) 소비 — 5칸 이동 시 1tick(=게임 내 5분)이라는
        // 기존 tick 경제 설계(HOMEPC_SYNC_NOTES.md)를 그대로 따른다. 새 상수 필요 없음.
        TickManager.Instance.CompleteTurn(this, TickCost.PerTileMove);
    }
}
