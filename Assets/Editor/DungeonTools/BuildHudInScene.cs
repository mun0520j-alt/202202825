using UnityEditor;
using UnityEngine;

// ================================================================================================
// [Summary] BuildHudInScene
// Dungeon Tools 10) 하이라키에 HUD(시계/인벤토리/미니맵) 미리 배치(2026-09-04 신규).
//
// Unity MCP 에디터 연결을 시도했지만(로컬 HTTP 서버는 떴음) 이 대화가 붙어있는 클라우드 세션
// 까지는 연결이 안 잡혀서, 대신 이 메뉴 하나로 "Play를 눌렀을 때 런타임 코드가 만드는 것과
// 똑같은 자리"에 DungeonClockDisplay/InventoryPanel/MinimapController를 Edit 모드에서 직접
// 만들어 씬에 저장해둔다. 이렇게 하면:
//   1) Play를 누르지 않아도 하이라키에서 바로 보이고 선택할 수 있다.
//   2) 사용자가 직접 슬롯 배경/아이콘 Image의 스프라이트나 Text의 폰트를 바꿔둘 수 있다
//      ("내가 다시 이미지하고 폰트 바꿀게" — 2026-09-04 사용자 요청).
//
// 세 컴포넌트 모두 [ExecuteAlways]가 붙어있고 Build()가 멱등하게(이미 하이라키에 있으면 새로
// 안 만들고 참조만 재연결) 짜여있어서(DungeonClockDisplay.cs/InventoryPanel.cs/
// MinimapController.cs의 BindExisting 참고), 이 메뉴를 여러 번 눌러도 중복 생성되지 않고,
// Play를 눌러도 여기서 만든 하이라키를 그대로 재사용한다 — 즉 여기서 사용자가 바꿔둔 스프라이트/
// 폰트가 Play할 때마다 덮어써지지 않는다.
//
// [2026-09-04 버그 수정] 처음 버전은 "컴포넌트가 없을 때만 AddComponent"만 했는데, 컴포넌트
// (예: InventoryPanel)는 host 오브젝트에 이미 붙어있고 그게 실제로 만들어둔 UI 하이라키(Canvas
// 아래 "InventoryPanel"이라는 자식 오브젝트 — 이름이 컴포넌트랑 같아서 헷갈리기 쉬움)만 하이라키에서
// 지운 경우, GetComponent가 여전히 non-null이라 AddComponent를 아예 안 타서 Build()가 다시 안
// 불리고 — 즉 "지웠다가 메뉴 다시 눌러도 아무것도 안 생기는" 상태가 됐었다(사용자 리포트).
// 고침: 컴포넌트가 있든 없든(새로 추가했든 이미 있었든) 매번 RebuildForEditor()를 명시적으로
// 호출한다 — Build()는 멱등하니 아무것도 안 지워졌으면 그냥 기존 참조만 재확인하고 끝나고,
// 자식이 지워졌으면 그 자리에서 다시 만들어준다.
// ================================================================================================
public static class BuildHudInScene
{
    [MenuItem("Dungeon Tools/10) Place HUD In Scene (Edit Mode)")]
    public static void PlaceHud()
    {
        // Player/Enemy처럼 씬에 여러 개 배치되는 오브젝트가 아니라 "씬 전역 HUD 관리자" 하나만
        // 있으면 되므로, 기존 DungeonSceneBootstrapper 오브젝트가 있으면 거기에 같이 붙인다
        // (실제 Play 시 그 오브젝트가 이 컴포넌트들을 갖게 되는 모양과 동일하게 맞추는 것) —
        // 없으면 새로 "HUD"라는 오브젝트를 만든다.
        var bootstrapper = Object.FindObjectOfType<DungeonSceneBootstrapper>();
        GameObject host = bootstrapper != null ? bootstrapper.gameObject : GameObject.Find("HUD");
        if (host == null)
        {
            host = new GameObject("HUD");
            Undo.RegisterCreatedObjectUndo(host, "Create HUD host");
        }

        int addedCount = 0;

        var clock = host.GetComponent<DungeonClockDisplay>();
        if (clock == null) { clock = Undo.AddComponent<DungeonClockDisplay>(host); addedCount++; }
        clock.RebuildForEditor(); // 컴포넌트가 이미 있었어도 하이라키 자식이 지워졌으면 여기서 복구된다.

        var inventory = host.GetComponent<InventoryPanel>();
        if (inventory == null) { inventory = Undo.AddComponent<InventoryPanel>(host); addedCount++; }
        inventory.RebuildForEditor();

        var minimap = host.GetComponent<MinimapController>();
        if (minimap == null) { minimap = Undo.AddComponent<MinimapController>(host); addedCount++; }
        minimap.RebuildForEditor();

        if (addedCount == 0)
        {
            Debug.Log("[BuildHudInScene] 컴포넌트는 이미 전부 있었습니다 — 하이라키 자식이 지워졌던 것들만 다시 만들었습니다(있었으면 그대로 재사용).");
        }
        else
        {
            Debug.Log($"[BuildHudInScene] HUD 컴포넌트 {addedCount}개를 새로 '{host.name}' 오브젝트에 배치했습니다. " +
                      "하이라키에서 Canvas 아래 InventoryPanel / DungeonClockDisplay / MinimapFrame을 펼쳐서 " +
                      "이미지/폰트를 직접 바꿀 수 있습니다.");
        }

        EditorUtility.SetDirty(host);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(host.scene);
    }
}
