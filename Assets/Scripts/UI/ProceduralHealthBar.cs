using UnityEngine;

// ================================================================================================
// [Summary] ProceduralHealthBar
// Player/Enemy 머리 위에 뜨는 막대형 HP바 — 별도 이미지 에셋 없이 흰 텍스처(ProceduralSprite)만
// 색상 틴트해서 배경(회색 반투명) + 채움(초록→빨강 그라데이션) 두 겹으로 그린다(2026-09-04 신규).
//
// 왜 하트 대신 막대인가: 하트는 Player처럼 화면 고정 UI 한 곳에만 있을 땐 괜찮지만, Enemy는
// 여러 마리가 동시에 화면에 있고 몹마다 최대HP가 달라서(goblin 5, Player 10처럼) 하트 개수가
// 들쭉날쭉해져 가독성이 떨어진다는 사용자 판단(2026-09-04)에 따라 Player/Enemy 둘 다 이 막대형
// 하나로 통일했다. 나중에 0x72 작가의 DungeonUI 팩(같은 아트 스타일의 HP바 스프라이트, CC0)을
// 받아오면, 지금 흰 사각형 대신 그 스프라이트로 배경/채움 이미지만 갈아끼우면 된다 — 이 클래스가
// 노출하는 API(SetFill)는 그대로 유지되므로 호출하는 쪽(PlayerTurnActor/EnemyTurnActor)은 안 바뀐다.
//
// 사용법: 대상 GameObject의 Awake에서 `gameObject.AddComponent<ProceduralHealthBar>()`로 붙이고,
// HP가 바뀔 때마다(초기값 포함) `SetFill(currentHP / (float)maxHP)`를 호출하면 된다 — 인스펙터
// 설정이나 프리팹 작업이 전혀 필요 없다(AlertIndicator와 동일한 "완전 자체 생성" 패턴).
// ================================================================================================
public class ProceduralHealthBar : MonoBehaviour
{
    [Header("크기/위치 (월드 유닛 기준)")]
    [SerializeField] private float width = 0.8f;
    [SerializeField] private float height = 0.12f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.9f, 0f); // 머리 위

    [Header("동작")]
    [Tooltip("체력이 가득 찼을 때(ratio>=1) 바를 아예 숨길지 여부 — 평소 화면을 깔끔하게 유지하고 싶을 때 켠다.")]
    [SerializeField] private bool hideWhenFull = false;

    private Transform barRoot;
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        barRoot = new GameObject("ProceduralHealthBar").transform;
        barRoot.SetParent(transform);
        barRoot.localPosition = localOffset;
        barRoot.localRotation = Quaternion.identity;
        barRoot.localScale = Vector3.one; // 부모(캐릭터) 스케일과 무관하게 항상 같은 크기로 보이도록 고정.

        // 배경 — 항상 꽉 찬 상태로 그려서 "빈 부분"의 테두리 역할을 한다.
        var backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(barRoot);
        backgroundGo.transform.localPosition = Vector3.zero;
        backgroundGo.transform.localScale = new Vector3(width, height, 1f);
        var backgroundRenderer = backgroundGo.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        backgroundRenderer.color = new Color(0f, 0f, 0f, 0.55f);
        backgroundRenderer.sortingOrder = 150; // 캐릭터 스프라이트/이펙트보다 위에 그려지도록 넉넉히 높게.

        // 채움 — 왼쪽 피벗 스프라이트를 배경의 왼쪽 끝에 맞춰 배치해서, 가로 스케일만 줄이면
        // 왼쪽은 고정된 채 오른쪽만 줄어드는(=일반적인 HP바 채움 방식) 결과가 나온다.
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot);
        fillGo.transform.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
        fillGo.transform.localScale = new Vector3(width, height * 0.7f, 1f); // 배경보다 살짝 얇게 — 테두리처럼 보이게.
        fillRenderer = fillGo.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = ProceduralSprite.GetWhiteSpriteLeftPivot();
        fillRenderer.color = Color.green;
        fillRenderer.sortingOrder = 151; // 배경 바로 위.
        fillTransform = fillGo.transform;
    }

    // currentHP/maxHP 같은 0~1 비율을 받아서 바를 갱신한다. 호출하는 쪽(Player/Enemy)이 HP 계산
    // 책임을 그대로 갖고, 이 클래스는 "받은 비율을 그림으로만 표현"하는 순수 뷰 역할만 한다.
    public void SetFill(float ratio01)
    {
        ratio01 = Mathf.Clamp01(ratio01);

        var scale = fillTransform.localScale;
        scale.x = width * ratio01;
        fillTransform.localScale = scale;

        // 초록(가득참) -> 빨강(위험) 그라데이션 — 별도 스프라이트 없이 색상 보간만으로 표현.
        fillRenderer.color = Color.Lerp(Color.red, Color.green, ratio01);

        if (hideWhenFull)
        {
            barRoot.gameObject.SetActive(ratio01 < 0.999f);
        }
    }
}
