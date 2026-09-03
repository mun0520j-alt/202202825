using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dungeon Tools 10) 가장 단순한 형태의 Enemy ITurnActor 구현체. Tick 파이프라인이 Player 외의
// 실제 액터에서도 end-to-end로 정상 동작하는지 검증하기 위한 약식(1차) 구현이다(2026-08-28).
//
// PlayerTurnActor와 이동/애니메이션 로직(벽 판정, 셀<->월드 좌표 변환, 홉 애니메이션)이 상당 부분
// 중복된다. 지금은 "파이프라인이 실제로 동작하는지 확인"이 최우선 목표라 의도적으로 중복을
// 허용한 것 — 이 로직이 정상 동작 확인되면, 그때 Player/Enemy 공통 이동 로직을 별도
// 컴포넌트/유틸리티로 뽑아내는 리팩터링을 진행할 예정(2026-08-28 사용자 결정).
//
// 등록/해제는 Player와 동일하게 self-register 방식을 쓴다(2026-08-28, 최초엔 "스폰 시스템이
// 생기면 그때 외부 등록 방식으로" 미뤄뒀다가 바로 수정 — Enemy도 씬에 배치만 해두면 자동으로
// 등록되는 게 검증에 필요했고, 생각해보니 나중에 스포너가 Instantiate()로 생성해도 OnEnable은
// 똑같이 자동 호출되니까 self-register가 Player 전용일 이유가 없었음). 스포너가 생기더라도
// 스포너 쪽에서 등록 코드를 따로 짤 필요 없이 Instantiate만 하면 이 OnEnable이 알아서 처리한다.
public class EnemyTurnActor : MonoBehaviour, ITurnActor
{
    [Header("행동 타입")]
    [Tooltip("이 몹이 자기 턴에 무엇을 할지 결정한다. 종류가 늘어나면 EnemyBehaviorType enum에 추가한다.")]
    [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Idle;

    [Header("타일맵 참조 (Inspector에서 MapGenPreview/Floor, /Walls 드래그해서 연결)")]
    [Tooltip("벽 충돌 판정용. 이 칸에 타일이 있으면 이동 불가.")]
    [SerializeField] private Tilemap wallsTilemap;
    [Tooltip("바닥 판정용 + 셀<->월드 좌표 변환 기준. 이 칸에 타일이 없으면(맵 바깥) 이동 불가.")]
    [SerializeField] private Tilemap floorTilemap;

    [Header("이동 연출 (PlayerTurnActor와 동일한 홉 애니메이션, 실시간 기준 — tick과 무관)")]
    [SerializeField] private float hopDuration = 0.12f;
    [SerializeField] private float hopHeight = 0.15f;

    [Header("시야/추적 (2026-08-28 추가)")]
    [Tooltip("이 몹이 Player를 감지할 수 있는 최대 거리(칸, 원형 반경) — 이 범위 밖이면 시야가 안 막혀도 못 본다.")]
    [SerializeField] private int sightRangeInTiles = 5;

    // [2026-09-03 신규] 전투 스탯 — PlayerTurnActor와 동일한 placeholder 목적(3단계 아이템/스탯
    // 시스템이 들어오기 전까지 임시). 몹마다 스탯만 다르게(2~3종) 두는 버티컬 슬라이스 방침이라
    // Inspector에서 몹 프리팹별로 값을 다르게 세팅하면 그대로 난이도 차등이 됨.
    [Header("전투 (2026-09-03 신규, placeholder — 인스펙터에서 확인용)")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private int currentHP = 5;
    [SerializeField] private int attackPower = 2;
    [SerializeField] private int defensePower = 0;

    // 발 위치 보정(Y)에 쓴다 — 아래 GetCellFootWorldPosition() 주석 참고.
    private SpriteRenderer spriteRenderer;

    // AI는 차례가 오면 즉시 판단해서 바로 CompleteTurn을 불러야 하는 게 정상 동작이라(Player와
    // 반대), 이 호출을 깜빡하는 버그가 생기면 TickManager 워치독이 곧바로 잡아내야 한다.
    public bool SuppressStuckTurnWarning => false;

    // ITurnActor.CurrentCell 구현 — TickManager.IsCellOccupied()가 Player/Enemy 겹침 방지를 위해
    // 조회한다(2026-08-28 추가, ITurnActor.cs 주석 참고).
    public Vector3Int CurrentCell => currentCell;

    // 4방향만(대각선 없음) — PlayerTurnActor와 동일한 이동 규칙(2026-08-27 확정).
    private static readonly Vector3Int[] FourDirections =
    {
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
    };

    private Vector3Int currentCell;
    private bool isAnimating;

    // behaviorType(Idle/Wander)은 "Player를 못 봤을 때의 평소 기본 행동"이고, 추적은 그 위에
    // 얹히는 임시 상태다(2026-08-28 결정, "감지 -> 추적 -> 놓치면 마지막 위치 -> 배회 복귀"
    // 요청 반영) — 그래서 EnemyBehaviorType에 별도 Trace 항목을 안 만들고 이 두 필드로
    // 상태를 따로 추적한다. isTracing=true면 매 턴 Player의 "현재" 셀로 다시 경로를 계산해서
    // 쫓아가고, 시야에서 놓치면 lastKnownPlayerCell(마지막으로 본 위치)까지만 마저 쫓아간 뒤
    // 거기 도착하면 isTracing=false로 돌아가 기본 행동(behaviorType)을 재개한다.
    private bool isTracing;
    private Vector3Int? lastKnownPlayerCell;

    private void Awake()
    {
        if (wallsTilemap == null || floorTilemap == null)
        {
            Debug.LogError("[EnemyTurnActor] wallsTilemap/floorTilemap이 Inspector에 연결 안 돼있습니다 — " +
                            "MapGenPreview 하위의 Walls/Floor 타일맵을 드래그해서 연결해주세요.");
            enabled = false;
            return;
        }

        // 배치된 월드 좌표를 논리 좌표(셀)로 역산 — PlayerTurnActor.Awake()와 동일한 방식.
        currentCell = floorTilemap.WorldToCell(transform.position);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[EnemyTurnActor] SpriteRenderer를 못 찾아서 발 위치 보정(Y) 없이 셀 바닥에 그대로 배치됩니다.");
        }
    }

    // PlayerTurnActor.OnEnable()/OnDisable()과 동일한 self-register 패턴(2026-08-28로 확정,
    // 위 클래스 주석 참고). TickManager가 아직 씬에 없으면 Player 쪽처럼 여기서도 만들어서
    // 등록해준다 — 씬 초기화 순서(Awake/OnEnable 실행 순서)에 무관하게 항상 안전하게 등록되게 함.
    private void OnEnable()
    {
        var tickManager = TickManager.Instance;
        if (tickManager == null)
        {
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

    // 매 턴 순서(2026-08-28 확정): 1) 지금 Player가 보이는지 확인 → 보이면 그쪽으로 추적.
    // 2) 안 보이지만 방금까지 추적 중이었다면 → 마지막 목격 위치까지 마저 이동(도착했으면 포기).
    // 3) 둘 다 아니면 → 평소 기본 행동(Idle/Wander).
    public void OnTurnStart()
    {
        if (TryDetectPlayer(out var playerCell))
        {
            isTracing = true;
            lastKnownPlayerCell = playerCell;
            TryTraceTowards(playerCell);
            return;
        }

        if (isTracing && lastKnownPlayerCell.HasValue)
        {
            if (currentCell == lastKnownPlayerCell.Value)
            {
                // 마지막 목격 위치에 도착했는데도 안 보임 — 추적 종료, 이번 턴부터는 기본 행동.
                isTracing = false;
                lastKnownPlayerCell = null;
            }
            else
            {
                TryTraceTowards(lastKnownPlayerCell.Value);
                return;
            }
        }

        RunDefaultBehavior();
    }

    // 평소(Player를 못 봤을 때) 기본 행동 — 원래 OnTurnStart에 있던 로직을 그대로 옮긴 것뿐,
    // 동작은 안 바뀜(2026-08-28 리팩터링).
    private void RunDefaultBehavior()
    {
        switch (behaviorType)
        {
            case EnemyBehaviorType.Wander:
                TryWanderStep();
                break;

            case EnemyBehaviorType.Idle:
            default:
                // 가만히 있어도 "이번 턴엔 안 움직이기로 확정"한 행동이라 tick은 그대로 소비한다.
                CompleteMyTurn();
                break;
        }
    }

    // 지금 Player가 시야 반경 안에 있고 + 벽에 안 막혀서 실제로 보이는지 확인한다
    // (LineOfSight.cs 참고). Player가 씬에 없으면(테스트 중 등) 당연히 false.
    private bool TryDetectPlayer(out Vector3Int playerCell)
    {
        playerCell = default;

        var player = PlayerTurnActor.Instance;
        if (player == null) return false;

        playerCell = player.CurrentCell;
        var delta = playerCell - currentCell;
        if (delta.x * delta.x + delta.y * delta.y > sightRangeInTiles * sightRangeInTiles) return false; // 원형 반경 밖

        return LineOfSight.HasLineOfSight(currentCell, playerCell, wallsTilemap);
    }

    // targetCell(추적 중인 Player의 현재 위치, 또는 마지막 목격 위치)까지 TilePathfinder로
    // 매 턴 새로 경로를 계산해서 첫 칸만 이동한다 — 캐싱 안 하는 이유: Player가 매 턴 움직여서
    // 목표 자체가 계속 바뀌니까, 옛 경로를 그대로 쓰면 엉뚱한 길로 갈 수 있다.
    private void TryTraceTowards(Vector3Int targetCell)
    {
        // [2026-09-03 버그 수정] Player.HandleMouseClick과 동일한 이유로 점유 칸을 우회해서
        // 경로를 짠다(TilePathfinder.cs 주석 참고) — 몹이 여러 마리로 늘어났을 때 서로를
        // 뚫고 지나가는 경로를 짜지 않도록 한다.
        var path = TilePathfinder.FindPath(currentCell, targetCell, wallsTilemap, floorTilemap,
            cell => TickManager.Instance.IsCellOccupied(cell, this));
        if (path.Count == 0)
        {
            // 갈 방법이 없음(완전히 막혔거나 이미 그 칸에 있음) — 추적을 포기하고 이번 턴은
            // 기본 행동으로 대체한다.
            isTracing = false;
            lastKnownPlayerCell = null;
            RunDefaultBehavior();
            return;
        }

        var nextCell = path[0];
        var blocker = TickManager.Instance.GetActorAt(nextCell, this);
        if (blocker is PlayerTurnActor player)
        {
            // [2026-09-03 신규] 다음 칸이 곧 Player가 서있는 칸이라는 뜻 — 이미 인접했으니
            // 이동 대신 공격으로 전환한다. 이동 없이 그 자리에서 데미지 판정만 하고 턴 소비.
            // 비용은 CompleteMyTurn()(이동/대기용 PerTileMove)이 아니라 TickCost.Attack — 공격은
            // Player.AttackEnemy()와 동일하게 항상 고정 비용(TickCost.cs 주석 참고, 속도 배율 영향 없음).
            player.TakeDamage(attackPower);
            TickManager.Instance.CompleteTurn(this, TickCost.Attack);
            return;
        }
        if (blocker != null)
        {
            // Player가 아닌 다른 액터(예: 다른 Enemy)가 막고 있음 — 몹끼리 상호작용은 아직
            // 범위 밖이라, 예전처럼 제자리에서 턴만 소비하고 넘어간다.
            CompleteMyTurn();
            return;
        }

        StartCoroutine(HopTo(nextCell));
    }

    // 4방향 중 무작위 순서로 하나씩 시도해서 처음 갈 수 있는 칸으로 이동한다. 최대
    // FourDirections.Length번만 시도하므로(무한루프 없음), 사방이 다 막혀있으면 이동 없이
    // 제자리에서 턴만 소비한다.
    private void TryWanderStep()
    {
        var shuffledDirections = (Vector3Int[])FourDirections.Clone();
        Shuffle(shuffledDirections);

        foreach (var dir in shuffledDirections)
        {
            var targetCell = currentCell + dir;
            if (wallsTilemap.HasTile(targetCell)) continue;
            if (!floorTilemap.HasTile(targetCell)) continue; // 맵 바깥으로는 못 나감
            // 벽/바닥과 같은 자격으로 "이미 다른 액터(Player 포함)가 서 있는 칸"도 막는다 —
            // PlayerTurnActor.TryStep()의 동일 체크와 같은 이유(콜라이더로는 안 막힘, 2026-08-28).
            if (TickManager.Instance.IsCellOccupied(targetCell, this)) continue;

            StartCoroutine(HopTo(targetCell));
            return; // 이동 성공 — 턴 소비/CompleteTurn 호출은 애니메이션이 끝난 뒤 HopTo 안에서 처리
        }

        // 사방이 다 막혀있으면(방이 1칸짜리 등) 이동 없이 턴만 소비.
        CompleteMyTurn();
    }

    // Fisher-Yates 셔플 — 4방향 중 무작위 하나를 고르는 대신 "무작위 순서로 4개를 다 시도"하는
    // 방식을 쓴 이유: 첫 시도가 막힌 벽이어도 바로 포기하지 않고 다른 방향도 마저 시도해서,
    // 갈 수 있는 칸이 하나라도 있으면 최대한 그쪽으로 움직이게 하기 위함(완전히 갇힌 경우만 정지).
    private void Shuffle(Vector3Int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // X는 PlayerTurnActor와 동일하게 셀 중앙(+0.5) — pivot이 가로 방향은 항상 센터라 폭이
    // 몹마다 달라도 이렇게만 하면 자동으로 셀 중앙에 맞는다.
    //
    // Y는 PlayerTurnActor와 다르게 계산한다(2026-08-28 발견/수정) — 원래는 Player처럼 "셀 원점
    // 그대로(보정 없음)"이었는데, goblin을 실제로 배치해보니 스프라이트가 타일 아래로 파묻혀
    // 보이는 문제가 있었다. 원인: 이 프로젝트의 몹 스프라이트는 전부 pivot이 Center(0.5, 0.5)로
    // 통일되어 있는데(PixelArtImportFixer.cs 임포트 규칙), 몹마다 스프라이트를 크롭한 실제 픽셀
    // 크기가 다르다(goblin 12x11px, knight_m 16x21px, big_demon 25x31px 등, 전부 16px=1유닛
    // 기준). pivot이 Center라 transform.position은 "스프라이트의 중심"을 가리키는데, 몹마다
    // 세로 크기가 다르니 발(스프라이트 하단)이 타일 바닥선에 오도록 하려면 몹마다 다른 만큼
    // 위로 올려줘야 한다 — 고정 상수로는 안 되고, 매번 스프라이트 자체에서 실제 세로 반높이
    // (spriteRenderer.sprite.bounds.extents.y, 월드 유닛 기준)를 읽어와야 몹 종류가 몇 개든
    // 자동으로 정확히 맞는다.
    private Vector3 GetCellFootWorldPosition(Vector3Int cell)
    {
        Vector3 cellOrigin = floorTilemap.CellToWorld(cell); // 셀의 바닥-왼쪽 모서리(정수 좌표)
        Vector3 cellSize = floorTilemap.cellSize;

        float footOffsetY = 0f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            footOffsetY = spriteRenderer.sprite.bounds.extents.y;
        }

        return cellOrigin + new Vector3(cellSize.x * 0.5f, footOffsetY, 0f);
    }

    // PlayerTurnActor.HopTo()와 동일한 사인 곡선 홉 애니메이션(네크로댄서 참고) — 기울임/찌그러짐
    // 없음, 수평은 선형 보간 + 수직만 사인 곡선을 얹는다(2026-08-27 확정 스타일).
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
            Vector3 flatPos = Vector3.Lerp(startPos, endPos, p);
            float hopOffset = Mathf.Sin(p * Mathf.PI) * hopHeight;
            transform.position = flatPos + new Vector3(0f, hopOffset, 0f);
            yield return null;
        }

        transform.position = endPos; // 부동소수점 오차 정리 — 정확히 목표 위치에 스냅.
        currentCell = targetCell;    // 논리 좌표는 애니메이션이 끝나는 시점에 확정한다.
        isAnimating = false;

        CompleteMyTurn();
    }

    // Idle이든 Wander(이동 성공/실패 불문)든, 지금은 전부 같은 비용(TickCost.PerTileMove)을
    // 쓰기로 확정했다(2026-08-28 결정) — 새 상수를 만들지 않고 "한 번의 확정된 행동" 비용으로
    // 기존 이동 비용을 그대로 재사용한다. 나중에 몹 종류가 늘어나 행동별 비용을 세분화해야
    // 하면 TickCost.cs에 항목을 추가하고 여기서 갈아끼우면 된다.
    private void CompleteMyTurn()
    {
        TickManager.Instance.CompleteTurn(this, TickCost.PerTileMove);
    }

    // [2026-09-03 신규] Player.AttackEnemy()가 호출한다. 데미지 공식은 PlayerTurnActor와 동일하게
    // "공격력 - 방어력"(0 밑으로는 안 내려감). HP가 0 이하가 되면 즉시 제거한다 — Player 쪽처럼
    // 한 프레임 미룰 필요가 없는 이유: 이 호출은 Player.Update()(수동 입력 처리) 안에서 오는
    // 거라 TickManager.AdvanceSchedule 반복문 도중이 아니고, 여기서 죽어도 이어서 실행되는 건
    // "내 턴을 넘긴다"가 아니라 Player 쪽 로직뿐이라 안전하다.
    //
    // UnregisterActor를 Destroy가 트리거하는 OnDisable 타이밍에 맡기지 않고 여기서 바로 부르는
    // 이유: Destroy()는 실제 파괴를 프레임 끝으로 미루는데, 그 사이 이 액터가 다시 차례를 받으려
    // 하면(등록이 아직 안 풀렸으니) 죽은 채로 OnTurnStart가 불릴 위험이 있다 — 즉시 해제해서 막는다.
    public void TakeDamage(int incomingAttackPower)
    {
        int damage = Mathf.Max(0, incomingAttackPower - defensePower);
        currentHP = Mathf.Max(0, currentHP - damage);

        if (currentHP <= 0)
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.UnregisterActor(this);
            }
            Destroy(gameObject);
        }
    }
}
