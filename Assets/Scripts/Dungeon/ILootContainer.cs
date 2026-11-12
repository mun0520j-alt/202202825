public interface ILootContainer
{
    string DisplayName { get; }
    LootGridModel Grid { get; }
    bool TryAdd(LootItemInstance item);
    bool TryRemove(string instanceId, out LootItemInstance item);
    void HandleContentsChanged();
}
