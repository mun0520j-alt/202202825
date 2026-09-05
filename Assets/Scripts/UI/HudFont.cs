using UnityEngine;

// ================================================================================================
// [Summary] HudFont
// uGUI Text 컴포넌트에 쓸 기본 폰트를 한 곳에서 구해오는 유틸리티(2026-09-04 신규) — 유니티
// 버전에 따라 내장 폰트 이름이 "Arial.ttf"였다가 "LegacyRuntime.ttf"로 바뀐 적이 있어서, 여러
// HUD 컴포넌트(시계, 인벤토리 등)가 각자 이 폴백 로직을 중복해서 짜지 않도록 한 군데로 모았다
// (ProceduralSprite와 같은 목적의 공용 유틸리티).
// ================================================================================================
public static class HudFont
{
    private static Font cachedFont;

    public static Font GetDefault()
    {
        if (cachedFont != null) return cachedFont;

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return cachedFont;
    }
}
