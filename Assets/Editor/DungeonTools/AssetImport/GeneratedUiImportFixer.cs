using UnityEditor;
using UnityEngine;

// ================================================================================================
// [Summary] GeneratedUiImportFixer
// GPT로 생성한 UI 스프라이트 시트를 잘라 만든 Assets/Resources/UI_Generated/ 폴더 전용 임포트
// 설정 자동화 스크립트(2026-09-04 신규). 기존 PixelArtImportFixer.cs는 TargetFolder가
// "Assets/Art"로 하드코딩되어 있는데 실제 폴더는 "Assets/Art_Asset"이라 사실상 지금까지 아무
// 파일에도 적용된 적이 없는 상태(발견만 해두고 아직 사용자에게 확인/수정 안 함) — 그 버그가 있는
// 파일을 건드려서 이미 세팅 끝난 0x72/Kyrise 임포트 설정을 잘못 건드릴 위험을 피하려고, 이 새
// 폴더 하나만 보는 별도 스크립트로 분리했다.
//
// Resources 폴더에 둔 이유: 이 스프라이트들은 인스펙터에 수동으로 드래그해서 참조를 거는 대신
// (프로젝트의 "완전 자체 생성" 패턴 — DungeonClockDisplay/InventoryPanel처럼 씬 설정 없이
// 코드에서 전부 만드는 방식) GeneratedUiSprites.cs가 런타임에 Resources.Load로 이름만으로
// 찾아서 쓴다. 0x72/Kyrise 원본 아트는 SpriteFrameSet 에디터 도구로 미리 구워두는 방식이라
// Resources가 필요 없었지만, 이번 건 코드에서 즉석으로 붙였다 뗐다 하는 UI 장식이라 성격이 다르다.
//
// 9-slice 테두리(spriteBorder)는 10배 확대한 이미지를 육안으로 측정해서 정했다:
//   - frame_thin_small / frame_thin_medium / frame_thick_medium / panel_dark_medium /
//     grid_3x3_frame: 사방 4px 균일한 테두리를 가진 단순 프레임이라 9-slice로 자연스럽게
//     늘어난다.
//   - frame_gem_small / panel_dark_gem_large: 위쪽 테두리 중앙에 다이아몬드 보석 장식이
//     튀어나와 있어서, 9-slice로 세로/가로를 늘리면 보석이 반복되거나 깨져 보인다. 그래서 이
//     둘은 9-slice(spriteBorder)를 적용하지 않고 "고정 크기로만 쓰는 장식용 패널" 취급 —
//     크기를 바꾸지 않는 헤더/장식 프레임 용도로만 쓰길 권장한다.
//   - bar_flat_*, segbar_*, hpbar_labeled_*, heart_*: 단순 사각형/아이콘이라 9-slice 대상이
//     아니다.
// ================================================================================================
public class GeneratedUiImportFixer : AssetPostprocessor
{
    private const string TargetFolder = "Assets/Resources/UI_Generated";

    // 파일명 -> 사방 균일 9-slice 테두리(px). 여기 없는 파일은 9-slice 없이 기본 Single로 임포트된다.
    private static readonly System.Collections.Generic.Dictionary<string, int> NineSliceBorders =
        new System.Collections.Generic.Dictionary<string, int>
        {
            { "frame_thin_small", 4 },
            { "frame_thin_medium", 4 },
            { "frame_thick_medium", 4 },
            { "panel_dark_medium", 4 },
            { "grid_3x3_frame", 4 },
        };

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace("\\", "/").Contains(TargetFolder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 16f; // 기존 0x72 타일셋과 동일한 PPU 규약을 맞춘다.

        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (NineSliceBorders.TryGetValue(fileName, out int border))
        {
            // spriteBorder는 TextureImporter.spriteBorder로 직접 설정 가능(단일 스프라이트 모드).
            importer.spriteBorder = new Vector4(border, border, border, border);
        }
    }
}
