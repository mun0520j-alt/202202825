#!/usr/bin/env bash
# 폴더 재구성 후 남은 옛날 위치 파일 정리 스크립트.
# 저장소 루트(C:\Users\munju\OneDrive\문서\202202825)에서 Git Bash로 실행:
#   bash delete_old_files.sh
#
# git rm이 파일 삭제 + git 스테이징(.meta 포함)까지 한 번에 처리함.
# 주의: 실행 전 Unity 에디터를 닫아두는 게 안전함(열려있으면 파일 잠금으로 삭제가 씹힐 수 있음).

set -e

git rm Assets/Scripts/Core/TickCost.cs Assets/Scripts/Core/TickCost.cs.meta
git rm Assets/Scripts/Core/ITurnActor.cs Assets/Scripts/Core/ITurnActor.cs.meta
git rm Assets/Scripts/Core/TickManager.cs Assets/Scripts/Core/TickManager.cs.meta
git rm Assets/Scripts/Core/DungeonClock.cs Assets/Scripts/Core/DungeonClock.cs.meta

git rm Assets/Scripts/Debug/DebugAnimatorStatePlayer.cs Assets/Scripts/Debug/DebugAnimatorStatePlayer.cs.meta
git rm Assets/Scripts/Debug/TickQueueTestActor.cs Assets/Scripts/Debug/TickQueueTestActor.cs.meta
git rm Assets/Scripts/Debug/TickQueueTestBootstrapper.cs Assets/Scripts/Debug/TickQueueTestBootstrapper.cs.meta

git rm Assets/Editor/DungeonTools/PixelArtImportFixer.cs Assets/Editor/DungeonTools/PixelArtImportFixer.cs.meta
git rm Assets/Editor/DungeonTools/TilesetFrameSlicer.cs Assets/Editor/DungeonTools/TilesetFrameSlicer.cs.meta
git rm Assets/Editor/DungeonTools/OrganizeKyriseIcons.cs Assets/Editor/DungeonTools/OrganizeKyriseIcons.cs.meta
git rm Assets/Editor/DungeonTools/AnimationClipBuilder.cs Assets/Editor/DungeonTools/AnimationClipBuilder.cs.meta
git rm Assets/Editor/DungeonTools/AnimatorControllerBuilder.cs Assets/Editor/DungeonTools/AnimatorControllerBuilder.cs.meta
git rm Assets/Editor/DungeonTools/PrefabBuilder.cs Assets/Editor/DungeonTools/PrefabBuilder.cs.meta
git rm Assets/Editor/DungeonTools/BuildAnimStatesAndPrefabs.cs Assets/Editor/DungeonTools/BuildAnimStatesAndPrefabs.cs.meta
git rm Assets/Editor/DungeonTools/BuildDebugPrefabPreviewScene.cs Assets/Editor/DungeonTools/BuildDebugPrefabPreviewScene.cs.meta
git rm Assets/Editor/DungeonTools/FloorLayoutPreviewWindow.cs Assets/Editor/DungeonTools/FloorLayoutPreviewWindow.cs.meta
git rm Assets/Editor/DungeonTools/FloorTilemapPreviewWindow.cs Assets/Editor/DungeonTools/FloorTilemapPreviewWindow.cs.meta
git rm Assets/Editor/DungeonTools/PlaceholderTileFactory.cs Assets/Editor/DungeonTools/PlaceholderTileFactory.cs.meta
git rm Assets/Editor/DungeonTools/OrganizeGeneratedAssets.cs Assets/Editor/DungeonTools/OrganizeGeneratedAssets.cs.meta

echo ""
echo "=== 삭제 결과 확인 ==="
git status --short

echo ""
echo "완료. 문제없으면:"
echo "  git commit -m \"8.27 chore: 스크립트 폴더 세부 분류\""
echo "  git push"
echo "그리고 Unity 켜서 Test.unity 열어 TickQueueTestBootstrapper 컴포넌트 살아있는지 확인해줘."
