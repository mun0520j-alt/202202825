using UnityEditor;
using UnityEngine;

// Dungeon Tools 1a) 픽셀아트 임포트 설정 일괄 정리.
// Assets/Art 아래 모든 텍스처를 Point 필터 + 무압축 + PPU 16으로 맞춘다.
// "타일이 이상해 보인다"는 문제(기본 Bilinear 블러, 압축 아티팩트, 잘못된 PPU)의
// 원인 대부분이 여기서 온다. 메뉴를 눌러야만 동작하며, 씬/하이라키는 건드리지 않는다.
public static class PixelArtImportFixer
{
    private const string TargetFolder = "Assets/Art";
    private const int PixelsPerUnit = 16;

    [MenuItem("Dungeon Tools/1a) Fix Pixel Art Import Settings")]
    public static void FixAll()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TargetFolder });
        int changed = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.spritePixelsPerUnit != PixelsPerUnit) { importer.spritePixelsPerUnit = PixelsPerUnit; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
                Debug.Log($"[PixelArtImportFixer] 수정됨: {path}");
            }
        }

        Debug.Log($"[PixelArtImportFixer] 완료 — Assets/Art 하위 텍스처 {guids.Length}개 중 {changed}개 설정을 " +
                  $"Point 필터 / 무압축 / PPU {PixelsPerUnit}로 맞췄습니다.");
    }
}
