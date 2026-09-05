using UnityEngine;

// ================================================================================================
// [Summary] ProceduralSprite
// HitImpactVfx.cs에서 쓰던 "흰색 1x1 텍스처를 스프라이트로 감싸서 캐싱" 로직을 공용으로 뽑아낸
// 유틸리티(2026-09-04 신규, ProceduralHealthBar 추가하면서 같은 로직이 또 필요해져서 분리).
// 절차적 UI/VFX(이미지 에셋 없이 SpriteRenderer 색상 틴트만으로 사각형을 그리는 것들)가 전부
// 이 하나의 흰 텍스처를 재사용한다 — 텍스처를 여러 번 새로 만들 필요가 없다.
//
// Pivot이 두 종류 필요한 이유: 중앙 피벗(0.5, 0.5)은 "그 자리에서 커지는" 이펙트(HitImpactVfx)에
// 맞고, 왼쪽 피벗(0, 0.5)은 "왼쪽 끝을 고정한 채 오른쪽만 줄어드는" HP바 채움 표현
// (ProceduralHealthBar)에 맞다 — 피벗이 다르면 같은 스케일 애니메이션이라도 결과가 완전히 달라서
// 스프라이트 자체를 두 종류로 캐싱해둔다.
//
// [2026-09-04 버그 수정] HP바가 화면에 아예 안 보이던 원인 — pixelsPerUnit을 100f로 고정해뒀는데,
// Texture2D.whiteTexture는 실제로는 아주 작은 정사각 텍스처(보통 4x4px)라서 만들어지는 스프라이트의
// "기본 크기"가 tex.width/100 ≈ 0.04 월드 유닛밖에 안 됐다. 그 위에 ProceduralHealthBar가
// localScale=(width=0.8, height=0.12, ...)를 곱하면 실제로는 0.8*0.04 ≈ 0.03 유닛짜리, 화면에서
// 1픽셀도 안 되는 크기가 되어 사실상 안 보였던 것(HitImpactVfx도 같은 이유로 원래 의도보다 훨씬
// 작게 나오고 있었을 것). 고정값 100f 대신 텍스처 자신의 픽셀 크기(tex.width)를 pixelsPerUnit으로
// 쓰면 스프라이트의 기본 크기가 정확히 1x1 월드 유닛이 되어서, 그 다음부터 곱하는 localScale
// 값(0.8, 0.12 등)이 곧바로 "월드 유닛 기준 실제 크기"와 일치하게 된다 — 이 클래스를 쓰는
// ProceduralHealthBar/HitImpactVfx 쪽 코드는 원래 그렇게 동작한다고 가정하고 짜여 있었으므로
// 여기만 고치면 나머지는 그대로 맞는다.
// ================================================================================================
public static class ProceduralSprite
{
    private static Sprite cachedCenterPivot;
    private static Sprite cachedLeftPivot;
    private static Sprite cachedCircle;

    // 스케일이 중앙 기준으로 커지고 작아지는 용도(예: 타격 이펙트) — HitImpactVfx가 사용.
    public static Sprite GetWhiteSpriteCenterPivot()
    {
        if (cachedCenterPivot != null) return cachedCenterPivot;
        var tex = Texture2D.whiteTexture;
        cachedCenterPivot = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), tex.width);
        return cachedCenterPivot;
    }

    // 왼쪽 끝이 고정된 채 가로 스케일만 줄어드는 용도(예: HP바 채움) — ProceduralHealthBar가 사용.
    public static Sprite GetWhiteSpriteLeftPivot()
    {
        if (cachedLeftPivot != null) return cachedLeftPivot;
        var tex = Texture2D.whiteTexture;
        cachedLeftPivot = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0f, 0.5f), tex.width);
        return cachedLeftPivot;
    }

    // [2026-09-04 신규] 아날로그 시계 문자판(DungeonClockDisplay)용 — 사각형 흰 텍스처로는 원을
    // 흉내낼 수 없어서, 픽셀 단위로 원 모양 알파 마스크를 직접 그린 전용 텍스처를 하나 만든다.
    // 안티에일리어싱은 가장자리 1px 폭에서만 부드럽게 처리(smoothstep 흉내)해서 계단현상을
    // 줄인다. 위 흰 텍스처들과 마찬가지로 pixelsPerUnit=텍스처 픽셀 크기로 맞춰서 기본 크기가
    // 정확히 1x1 월드/UI 유닛이 되게 한다(HP바 버그와 같은 실수를 반복하지 않기 위함).
    public static Sprite GetCircleSprite()
    {
        if (cachedCircle != null) return cachedCircle;

        const int diameter = 128;
        var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float radius = diameter * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        var pixels = new Color32[diameter * diameter];
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                // 가장자리 1.5px 폭에서만 부드럽게 알파를 깎아서(0~1 선형) 계단현상을 줄인다 —
                // 그 안쪽은 완전 불투명 흰색.
                float alpha = Mathf.Clamp01(radius - dist + 1.5f) / 1.5f;
                alpha = Mathf.Clamp01(alpha);
                pixels[y * diameter + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        cachedCircle = Sprite.Create(tex, new Rect(0f, 0f, diameter, diameter),
            new Vector2(0.5f, 0.5f), diameter);
        return cachedCircle;
    }
}
