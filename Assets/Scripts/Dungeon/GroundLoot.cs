using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class GroundLoot : MonoBehaviour
{
    private LootItemInstance item;
    private Vector3Int cell;

    public void Initialize(Tilemap floor, Vector3Int targetCell, LootItemInstance loot)
    {
        item = loot;
        cell = targetCell;
        transform.position = floor.GetCellCenterWorld(cell);
        name = $"GroundLoot({loot.Definition.Name})";

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralSprite.GetWhiteSpriteCenterPivot();
        renderer.color = RarityColor(loot.Definition.Rarity);
        renderer.sortingOrder = 6;
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);
    }

    private void Update()
    {
        var player = PlayerTurnActor.Instance;
        if (item == null || player == null || !Input.GetKeyDown(KeyCode.E)) return;
        if (Manhattan(player.CurrentCell, cell) > 1) return;

        if (!LootRuntimeState.TryAddToRunBag(item))
        {
            Debug.Log("[Loot] 가방이 가득 차 아이템을 바닥에 남겼습니다.");
            return;
        }

        Debug.Log($"[Loot] {item.Definition.Name} ({item.Definition.Rarity}) 획득 — {LootRuntimeState.RunBag.Count}/{LootRuntimeState.BagCapacity}");
        item = null;
        Destroy(gameObject);
    }

    private void OnGUI()
    {
        var player = PlayerTurnActor.Instance;
        if (item == null || player == null || Manhattan(player.CurrentCell, cell) > 1) return;
        string suffix = LootRuntimeState.RunBag.Count >= LootRuntimeState.BagCapacity ? " (가방 가득 참)" : string.Empty;
        GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 135f, 380f, 36f),
            $"E: {item.Definition.Name} [{item.Definition.Rarity}] 줍기{suffix}");
    }

    private static int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Color RarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Rare: return new Color(0.25f, 0.55f, 1f);
            case LootRarity.Epic: return new Color(0.75f, 0.3f, 1f);
            case LootRarity.Unique: return new Color(1f, 0.55f, 0.1f);
            case LootRarity.Legendary: return new Color(1f, 0.2f, 0.15f);
            default: return new Color(0.85f, 0.85f, 0.85f);
        }
    }
}
