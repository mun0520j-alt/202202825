using System.Collections;
using UnityEngine;

// ================================================================================================
// [Summary] HitImpactVfx
// 공격이 명중했을 때의 최소한의 시각 피드백 — 별도 이미지 에셋 없이 절차적으로 만든다
// (2026-09-04 신규, 사용자 요청: "공격할 때 상대 바라보기 + vfx"). 지금은 3단계(아이템/스탯)
// 이전이라 정식 이펙트 스프라이트가 없는 상태라, 나중에 진짜 에셋이 들어오면 이 클래스의
// Spawn() 내부만 교체하면 된다(호출하는 쪽 — Player/EnemyTurnActor.TakeDamage — 은 안 바뀜).
//
// 두 가지를 동시에 한다:
//   1) 맞은 위치에 흰색 사각형이 잠깐 커지면서 사라지는 "충격 이펙트" (Spawn)
//   2) 맞은 대상의 스프라이트 자체가 잠깐 빨갛게 물들었다가 원래 색으로 돌아옴 (FlashSprite)
// 둘 다 Texture2D.whiteTexture 하나로 처리해서 에셋 의존성이 전혀 없다.
// ================================================================================================
public static class HitImpactVfx
{
    // 흰색 1x1 텍스처 스프라이트는 ProceduralSprite(공용 유틸리티, 2026-09-04 분리)가 캐싱해서
    // 제공한다 — ProceduralHealthBar와 동일한 스프라이트를 재사용하므로 여기서 따로 만들지 않는다.

    // worldPos에 작은 정사각형을 만들어서 커지면서 투명해지는 애니메이션 후 자동 파괴한다.
    // 이 GameObject 자신이 코루틴을 돌리므로(HitImpactVfxRunner), 호출하는 쪽(Player/Enemy)이
    // MonoBehaviour 코루틴 host를 따로 챙길 필요가 없다 — "때린 순간 한 줄만 호출하면 끝"이 목표.
    public static void Spawn(Vector3 worldPos, Color color)
    {
        var go = new GameObject("HitImpactVfx");
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * 0.35f;

        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 200; // 몹/플레이어 스프라이트, "!" 표시보다도 위에 그려지도록.

        var runner = go.AddComponent<HitImpactVfxRunner>();
        runner.Play();
    }

    // targetRenderer의 색을 잠깐 flashColor로 바꿨다가 원래 색으로 복구한다. targetRenderer가
    // 속한 오브젝트가 이 프레임에 이미 죽어서 사라지는 경우(예: Enemy가 이 공격으로 즉사)도
    // 있으므로, host(코루틴을 실제로 돌릴 MonoBehaviour — 보통 맞은 당사자 자신)가 이미
    // 파괴되었으면 조용히 무시한다.
    public static void FlashSprite(MonoBehaviour host, SpriteRenderer targetRenderer, Color flashColor, float duration = 0.12f)
    {
        if (host == null || targetRenderer == null) return;
        host.StartCoroutine(FlashRoutine(targetRenderer, flashColor, duration));
    }

    private static IEnumerator FlashRoutine(SpriteRenderer targetRenderer, Color flashColor, float duration)
    {
        var originalColor = targetRenderer.color;
        targetRenderer.color = flashColor;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // targetRenderer가 이 사이에 파괴됐으면(피격 즉사 등) 더 진행할 필요 없이 바로 종료.
            if (targetRenderer == null) yield break;
            targetRenderer.color = Color.Lerp(flashColor, originalColor, t / duration);
            yield return null;
        }

        if (targetRenderer != null) targetRenderer.color = originalColor;
    }

    // Spawn()이 만든 임시 오브젝트 전용 — 확대되며 페이드아웃하는 애니메이션만 담당하고
    // 끝나면 자기 자신을 파괴한다.
    private class HitImpactVfxRunner : MonoBehaviour
    {
        private const float Duration = 0.18f;
        private const float EndScaleMultiplier = 2.4f;

        public void Play() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            var baseScale = transform.localScale;
            var baseColor = spriteRenderer.color;

            float t = 0f;
            while (t < Duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / Duration);
                transform.localScale = baseScale * Mathf.Lerp(1f, EndScaleMultiplier, p);
                var c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, p);
                spriteRenderer.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
