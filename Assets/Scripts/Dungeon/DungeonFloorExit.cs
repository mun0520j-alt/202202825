using UnityEngine;

// 현재 InDungeon 씬이 실제 사용하는 DungeonRunManager/GridMover용 M1 출구다.
public sealed class DungeonFloorExit : MonoBehaviour
{
    private DungeonRunManager runManager;
    private Transform player;
    private int floorNumber;
    private bool requestSent;

    public void Initialize(DungeonRunManager manager, Transform playerTransform, int floor)
    {
        runManager = manager;
        player = playerTransform;
        floorNumber = floor;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = floor >= ExpeditionSession.FinalFloor
            ? new Color(0.2f, 1f, 0.75f)
            : new Color(1f, 0.75f, 0.15f);
        renderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.7f, 0.7f, 1f);
    }

    private bool PlayerIsOnExit()
    {
        if (player == null || floorNumber != ExpeditionSession.CurrentFloor) return false;
        return Vector2.Distance(player.position, transform.position) <= 0.6f;
    }

    private void Update()
    {
        if (requestSent || !PlayerIsOnExit() || !Input.GetKeyDown(KeyCode.E)) return;
        requestSent = true;
        runManager.UseFloorExit(floorNumber);
    }

    private void OnGUI()
    {
        if (requestSent || !PlayerIsOnExit()) return;
        string label = floorNumber >= ExpeditionSession.FinalFloor
            ? $"E: 탈출 절차 ({runManager.EscapeProcedureTickCost} Tick)"
            : $"E: {floorNumber + 1}층으로 하강 ({TickCost.Interact:0} Tick)";
        GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 40f), label);
    }
}
