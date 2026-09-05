using UnityEngine;

// Dungeon Tools 9) 실제 던전 씬(DungeonScene)의 진입점 — TickManager/DungeonClock을 준비해두고
// 스케줄을 시작한다. 이름에 일부러 "Test"를 안 붙였다 — Assets/Scripts/Debug/TickQueueTest/
// TickQueueTestBootstrapper(검증용 더미 액터 3개로 스케줄러 자체를 테스트하는 용도)와 헷갈리지
// 않도록, 이쪽은 "실제 게임 로직이 사용하는 진짜 진입점"이라는 걸 이름으로 구분한다
// (2026-08-27 설계 확정: "debug용이랑 실제랑 이름만 다르게").
//
// 이 컴포넌트 자체는 액터를 하나도 만들지 않는다 — 씬에 배치된 PlayerTurnActor가 자기 자신을
// OnEnable에서 TickManager에 등록하므로(자기관리형, 플레이어는 항상 씬에 존재한다는 전제),
// 여기서는 그 등록이 끝난 뒤 스케줄만 시작해주면 된다.
public class DungeonSceneBootstrapper : MonoBehaviour
{
    private void Start()
    {
        // Unity 초기화 순서상 모든 오브젝트의 Awake/OnEnable이 어떤 Start()보다도 먼저 끝나기
        // 때문에, 이 Start()가 실행되는 시점엔 PlayerTurnActor.OnEnable이 이미 TickManager를
        // 만들어서 등록까지 마친 상태다 — 여기서는 그 인스턴스를 그대로 재사용한다.
        var tickManager = TickManager.Instance;
        if (tickManager == null)
        {
            // 혹시 씬에 액터가 하나도 없어서 TickManager가 아직 안 만들어진 경우를 대비한 방어 코드.
            Debug.LogWarning("[DungeonSceneBootstrapper] TickManager.Instance가 없어서 새로 만듭니다 — " +
                              "씬에 PlayerTurnActor 같은 액터가 하나도 없는 상태일 수 있습니다.");
            tickManager = gameObject.AddComponent<TickManager>();
        }

        var dungeonClock = tickManager.GetComponent<DungeonClock>();
        if (dungeonClock == null)
        {
            dungeonClock = tickManager.gameObject.AddComponent<DungeonClock>();
        }
        dungeonClock.ResetForNewRun();

        tickManager.BeginSchedule();

        // [2026-09-04 신규] HUD(시계/인벤토리/미니맵) 부착 — 전부 "완전 자체 생성" 컴포넌트라
        // (ScreenSpaceCanvasProvider.cs 참고) 여기서 AddComponent만 해주면 Canvas부터 텍스트/
        // 카메라까지 알아서 만들어진다. Player/Enemy에 HP바를 붙이는 것과 동일한 이유로, 씬에
        // 미리 배치해둘 필요 없이 이 부트스트래퍼가 자동으로 붙여준다 — 매 씬마다 수동으로
        // GameObject를 만들어 컴포넌트를 드래그할 필요가 없다.
        //
        // [2026-09-04 하이라키 배치 지원] 이제 BuildHudInScene.cs(에디터 메뉴)로 이 셋을 씬에
        // 미리 배치해둘 수도 있게 됐다 — 그렇게 미리 배치된 경우 여기서 또 AddComponent를 하면
        // 같은 오브젝트에 컴포넌트가 중복으로 붙어서 Awake가 두 번 도는 꼴이 된다(각 컴포넌트의
        // Build()가 자체적으로 멱등하게 짜여있어도,애초에 중복 컴포넌트 자체가 지저분하다).
        // FindObjectOfType으로 이미 있는지 먼저 확인해서 없을 때만 새로 붙인다.
        if (FindObjectOfType<DungeonClockDisplay>() == null) gameObject.AddComponent<DungeonClockDisplay>();
        if (FindObjectOfType<InventoryPanel>() == null) gameObject.AddComponent<InventoryPanel>();
        if (FindObjectOfType<MinimapController>() == null) gameObject.AddComponent<MinimapController>();

        Debug.Log("[DungeonSceneBootstrapper] 던전 씬 스케줄 시작 — TickManager/DungeonClock/HUD 준비 완료.");
    }
}
