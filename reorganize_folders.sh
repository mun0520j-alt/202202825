#!/usr/bin/env bash
# 폴더 재구성 스크립트 — git mv로 .cs와 .cs.meta를 항상 같이 옮겨서 GUID(씬 참조 포함)를 보존한다.
# Git Bash에서 저장소 루트(C:\Users\munju\OneDrive\문서\202202825)로 이동한 뒤 실행:
#   bash reorganize_folders.sh
#
# 주의: Unity 에디터를 켜놓은 상태로 실행해도 되지만, 에디터가 파일을 잠그고 있으면 실패할 수 있음.
# 안전하게는 Unity를 잠깐 닫고 실행 → 다시 열기를 권장.

set -e  # 중간에 하나라도 실패하면 즉시 멈춤 (부분 반영 방지)

echo "=== Core/ -> Core/Tick/ ==="
git mv Assets/Scripts/Core/TickCost.cs      Assets/Scripts/Core/Tick/TickCost.cs
git mv Assets/Scripts/Core/ITurnActor.cs    Assets/Scripts/Core/Tick/ITurnActor.cs
git mv Assets/Scripts/Core/TickManager.cs   Assets/Scripts/Core/Tick/TickManager.cs
git mv Assets/Scripts/Core/DungeonClock.cs  Assets/Scripts/Core/Tick/DungeonClock.cs

echo "=== Debug/ -> Debug/AnimationPreview/, Debug/TickQueueTest/ ==="
git mv Assets/Scripts/Debug/DebugAnimatorStatePlayer.cs   Assets/Scripts/Debug/AnimationPreview/DebugAnimatorStatePlayer.cs
git mv Assets/Scripts/Debug/TickQueueTestActor.cs         Assets/Scripts/Debug/TickQueueTest/TickQueueTestActor.cs
git mv Assets/Scripts/Debug/TickQueueTestBootstrapper.cs  Assets/Scripts/Debug/TickQueueTest/TickQueueTestBootstrapper.cs

echo "=== Editor/DungeonTools/ -> 4개 하위 폴더 ==="
git mv Assets/Editor/DungeonTools/PixelArtImportFixer.cs   Assets/Editor/DungeonTools/AssetImport/PixelArtImportFixer.cs
git mv Assets/Editor/DungeonTools/TilesetFrameSlicer.cs     Assets/Editor/DungeonTools/AssetImport/TilesetFrameSlicer.cs
git mv Assets/Editor/DungeonTools/OrganizeKyriseIcons.cs    Assets/Editor/DungeonTools/AssetImport/OrganizeKyriseIcons.cs

git mv Assets/Editor/DungeonTools/AnimationClipBuilder.cs     Assets/Editor/DungeonTools/AnimAndPrefab/AnimationClipBuilder.cs
git mv Assets/Editor/DungeonTools/AnimatorControllerBuilder.cs Assets/Editor/DungeonTools/AnimAndPrefab/AnimatorControllerBuilder.cs
git mv Assets/Editor/DungeonTools/PrefabBuilder.cs             Assets/Editor/DungeonTools/AnimAndPrefab/PrefabBuilder.cs
git mv Assets/Editor/DungeonTools/BuildAnimStatesAndPrefabs.cs Assets/Editor/DungeonTools/AnimAndPrefab/BuildAnimStatesAndPrefabs.cs

git mv Assets/Editor/DungeonTools/BuildDebugPrefabPreviewScene.cs Assets/Editor/DungeonTools/DebugPreview/BuildDebugPrefabPreviewScene.cs

git mv Assets/Editor/DungeonTools/FloorLayoutPreviewWindow.cs  Assets/Editor/DungeonTools/MapGenPreview/FloorLayoutPreviewWindow.cs
git mv Assets/Editor/DungeonTools/FloorTilemapPreviewWindow.cs Assets/Editor/DungeonTools/MapGenPreview/FloorTilemapPreviewWindow.cs
git mv Assets/Editor/DungeonTools/PlaceholderTileFactory.cs    Assets/Editor/DungeonTools/MapGenPreview/PlaceholderTileFactory.cs
git mv Assets/Editor/DungeonTools/OrganizeGeneratedAssets.cs   Assets/Editor/DungeonTools/MapGenPreview/OrganizeGeneratedAssets.cs

echo "=== 변경 결과 확인 ==="
git status --short

echo ""
echo "완료. 위 목록이 예상대로면:"
echo "  git add -A"
echo "  git commit -m \"8.27 chore: 스크립트 폴더 세부 분류\""
echo "  git push"
echo "그 다음 Unity 켜서(닫아뒀다면) Test.unity 열어서 TickQueueTestBootstrapper 참조가 살아있는지(Missing (Script) 안 뜨는지) 확인해줘."
