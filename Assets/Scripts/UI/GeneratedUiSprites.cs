using System.Collections.Generic;
using UnityEngine;

// ================================================================================================
// [Summary] GeneratedUiSprites
// GPT로 생성한 UI 스프라이트(Assets/Resources/UI_Generated/, GeneratedUiImportFixer.cs가 임포트
// 세팅을 맞춰줌)를 이름만으로 불러오는 캐싱 유틸리티(2026-09-04 신규). ProceduralSprite.cs가
// "코드로 직접 그린" 흰 사각형/원을 캐싱해주는 것과 같은 역할이되, 이쪽은 실제 PNG 에셋을
// Resources.Load로 불러온다는 점만 다르다.
//
// InventoryPanel처럼 "인스펙터 설정 없이 코드에서 전부 만드는" 컴포넌트가 실제 아트 에셋을
// 참조하려면 인스펙터 드래그가 필요한데, 그러면 프로젝트의 "완전 자체 생성" 패턴이 깨진다.
// Resources 폴더 + 이름 기반 로드로 그 문제를 피한다 — 씬/프리팹에 아무 참조도 안 남는다.
//
// 못 찾으면(아직 임포트가 안 됐거나 폴더가 비어있거나) null을 반환한다 — 호출하는 쪽은 항상
// null 체크 후 폴백(ProceduralSprite의 절차적 사각형 등)을 쓰도록 짜여 있어야 한다. 그래야
// 아직 에셋이 없는 초기 개발 단계에서도 에러 없이 예전 절차적 UI 그대로 보인다.
// ================================================================================================
public static class GeneratedUiSprites
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // 9-slice 테두리가 적용된(늘려도 안 깨지는) 프레임류 — 패널/슬롯 배경에 쓴다.
    public const string FrameThinSmall = "frame_thin_small";
    public const string FrameThinMedium = "frame_thin_medium";
    public const string FrameThickMedium = "frame_thick_medium";
    public const string PanelDarkMedium = "panel_dark_medium";
    public const string Grid3x3Frame = "grid_3x3_frame";

    // 9-slice 없이 고정 크기로만 쓰는 장식용(보석 장식이 늘리면 깨짐) — GeneratedUiImportFixer.cs 참고.
    public const string FrameGemSmall = "frame_gem_small";
    public const string PanelDarkGemLarge = "panel_dark_gem_large";

    public static Sprite Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var cached)) return cached;

        var sprite = Resources.Load<Sprite>($"UI_Generated/{name}");
        cache[name] = sprite; // 못 찾아도 null로 캐싱 — 매 프레임 반복 Resources.Load 시도를 막는다.
        if (sprite == null)
        {
            Debug.LogWarning($"[GeneratedUiSprites] '{name}' 스프라이트를 Resources/UI_Generated에서 못 찾았습니다 — 절차적 폴백을 대신 씁니다.");
        }
        return sprite;
    }
}
