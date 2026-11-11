using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// LootRuntimeState가 상태를 소유하고, 이 클래스는 저장 파일 직렬화와 교체만 담당한다.
public static class M5SaveService
{
    private const int CurrentSaveVersion = 2;
    private const string SaveFolderName = "Save";
    private const string SaveFileName = "save.json";
    private const string BackupFileName = "save.bak";
    private const string TempFileName = "save.tmp";

    private static bool isRestoring;
    private static string recoveryNotice;
    private static SaveSettings currentSettings = SaveSettings.Default();

    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolderName);
    public static string SavePath => Path.Combine(SaveDirectory, SaveFileName);
    public static string BackupPath => Path.Combine(SaveDirectory, BackupFileName);
    public static bool Fullscreen => currentSettings.fullscreen;
    public static int ResolutionWidth => currentSettings.resolution_width;
    public static int ResolutionHeight => currentSettings.resolution_height;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LootRuntimeState.Changed -= HandleLootChanged;
        Load();
        LootRuntimeState.Changed += HandleLootChanged;
    }

    public static void PrepareForExpedition()
    {
        var lossState = Capture(expeditionInProgress: true, includeExpeditionItems: false);
        Write(lossState);
    }

    public static void CompleteExpedition()
    {
        Write(Capture(expeditionInProgress: false, includeExpeditionItems: true));
    }

    public static void SaveNow()
    {
        Write(Capture(expeditionInProgress: ExpeditionSession.IsActive, includeExpeditionItems: !ExpeditionSession.IsActive));
    }

    public static void ApplySavedDisplaySettings()
    {
        if (currentSettings.resolution_width <= 0 || currentSettings.resolution_height <= 0)
            currentSettings = SaveSettings.Default();
        var available = Screen.resolutions;
        if (available.Length > 0 && !available.Any(value =>
                value.width == currentSettings.resolution_width && value.height == currentSettings.resolution_height))
        {
            Debug.LogWarning($"[Settings] 저장된 해상도 {currentSettings.resolution_width}x{currentSettings.resolution_height}를 사용할 수 없어 현재 화면으로 복귀합니다.");
            currentSettings.resolution_width = Mathf.Max(640, Screen.width);
            currentSettings.resolution_height = Mathf.Max(360, Screen.height);
        }
        Screen.SetResolution(currentSettings.resolution_width, currentSettings.resolution_height, currentSettings.fullscreen);
    }

    public static void UpdateDisplaySettings(int width, int height, bool fullscreen)
    {
        currentSettings.fullscreen = fullscreen;
        currentSettings.resolution_width = Mathf.Max(640, width);
        currentSettings.resolution_height = Mathf.Max(360, height);
        Screen.SetResolution(currentSettings.resolution_width, currentSettings.resolution_height, fullscreen);

        SaveData data = TryRead(SavePath) ?? TryRead(BackupPath)
            ?? Capture(expeditionInProgress: ExpeditionSession.IsActive, includeExpeditionItems: !ExpeditionSession.IsActive);
        data.settings = currentSettings.Clone();
        Write(data);
    }

    public static string ConsumeRecoveryNotice()
    {
        string notice = recoveryNotice;
        recoveryNotice = null;
        return notice;
    }

    private static void HandleLootChanged()
    {
        if (isRestoring || ExpeditionSession.IsActive) return;
        Write(Capture(expeditionInProgress: false, includeExpeditionItems: true));
    }

    private static void Load()
    {
        SaveData data = TryRead(SavePath);
        bool recoveredFromBackup = false;
        if (data == null)
        {
            data = TryRead(BackupPath);
            recoveredFromBackup = data != null;
        }

        if (data == null)
        {
            if (File.Exists(SavePath) || File.Exists(BackupPath))
                Debug.LogWarning("[Save] 주 세이브와 백업을 읽지 못해 새 게임으로 시작합니다.");
            return;
        }

        if (recoveredFromBackup) Debug.LogWarning("[Save] save.json이 손상되어 save.bak으로 복구했습니다.");
        Restore(data);
        if (!data.expedition_in_progress) return;

        recoveryNotice = "이전 원정이 종료되어 MIA 처리되었습니다.";
        data.expedition_in_progress = false;
        data.saved_at = DateTime.UtcNow.ToString("O");
        Write(data);
        Debug.LogWarning("[Save] 종료되지 않은 원정을 MIA로 처리했습니다.");
    }

    private static SaveData TryRead(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.save_version <= 0 || data.save_version > CurrentSaveVersion)
            {
                Debug.LogWarning($"[Save] 지원하지 않는 세이브 버전입니다: {data?.save_version ?? 0} ({path})");
                return null;
            }
            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Save] 세이브 읽기 실패: {path}\n{exception.Message}");
            return null;
        }
    }

    private static void Restore(SaveData data)
    {
        isRestoring = true;
        try
        {
            LootRuntimeState.RestorePersistentState(
                RestoreItems(data.run_bag),
                RestoreItems(data.stash),
                RestoreItems(data.equipped),
                RestoreBasicSlots(data.unequipped_basic_slots),
                data.has_basic_loadout,
                data.gold);
            currentSettings = data.settings != null ? data.settings.Normalized() : SaveSettings.Default();
        }
        finally
        {
            isRestoring = false;
        }
    }

    private static List<LootItemInstance> RestoreItems(List<SavedItem> savedItems)
    {
        var result = new List<LootItemInstance>();
        if (savedItems == null) return result;
        foreach (var saved in savedItems)
        {
            LootItemData definition = ResolveDefinition(saved.item_id);
            if (definition == null)
            {
                Debug.LogWarning($"[Save] CSV에 없는 item_id를 건너뜁니다: {saved.item_id}");
                continue;
            }
            result.Add(new LootItemInstance(definition, saved.instance_id));
        }
        return result;
    }

    private static IEnumerable<LootItemType> RestoreBasicSlots(List<string> slots)
    {
        if (slots == null) yield break;
        foreach (string slot in slots)
        {
            if (Enum.TryParse(slot, out LootItemType type)) yield return type;
        }
    }

    private static LootItemData ResolveDefinition(string itemId)
    {
        LootItemData csvItem = LootItemDatabase.FindById(itemId);
        if (csvItem != null) return csvItem;
        switch (itemId)
        {
            case "basic_weapon": return Basic("basic_weapon", "기본 검", LootItemType.Weapon, 20, 0);
            case "basic_shield": return Basic("basic_shield", "낡은 방패", LootItemType.Shield, 0, 6);
            case "basic_armor": return Basic("basic_armor", "가죽 갑옷", LootItemType.Armor, 0, 6);
            default: return null;
        }
    }

    private static LootItemData Basic(string id, string name, LootItemType type, int attack, int defense)
    {
        return new LootItemData
        {
            Id = id, Name = name, Type = type, Rarity = LootRarity.Normal,
            AttackBonus = attack, DefenseBonus = defense, CapacityBonus = 0,
            IconKey = string.Empty, DropWeight = 0,
            GridWidth = type == LootItemType.Weapon ? 1 : 2,
            GridHeight = type == LootItemType.Shield ? 2 : 3
        };
    }

    private static SaveData Capture(bool expeditionInProgress, bool includeExpeditionItems)
    {
        return new SaveData
        {
            save_version = CurrentSaveVersion,
            saved_at = DateTime.UtcNow.ToString("O"),
            stash = SaveItems(LootRuntimeState.Stash),
            run_bag = includeExpeditionItems ? SaveItems(LootRuntimeState.RunBag) : new List<SavedItem>(),
            equipped = includeExpeditionItems ? SaveItems(LootRuntimeState.EquippedLoot.Values) : new List<SavedItem>(),
            has_basic_loadout = includeExpeditionItems && LootRuntimeState.HasBasicLoadout,
            unequipped_basic_slots = includeExpeditionItems
                ? Enum.GetValues(typeof(LootItemType)).Cast<LootItemType>()
                    .Where(LootRuntimeState.IsBasicSlotUnequipped).Select(type => type.ToString()).ToList()
                : new List<string>(),
            settings = currentSettings.Clone(),
            expedition_in_progress = expeditionInProgress
            ,gold = LootRuntimeState.Gold
        };
    }

    private static List<SavedItem> SaveItems(IEnumerable<LootItemInstance> items)
    {
        return items.Where(item => item?.Definition != null).Select(item => new SavedItem
        {
            item_id = item.Definition.Id,
            instance_id = item.InstanceId
        }).ToList();
    }

    private static bool Write(SaveData data)
    {
        string tempPath = Path.Combine(SaveDirectory, TempFileName);
        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(SavePath))
            {
                try
                {
                    File.Replace(tempPath, SavePath, BackupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(SavePath, BackupPath, true);
                    File.Delete(SavePath);
                    File.Move(tempPath, SavePath);
                }
            }
            else
            {
                File.Move(tempPath, SavePath);
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 저장 실패. 기존 세이브는 유지됩니다.\n{exception.Message}");
            return false;
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (Exception) { }
        }
    }

    [Serializable]
    private sealed class SaveData
    {
        public int save_version = CurrentSaveVersion;
        public string saved_at;
        public List<SavedItem> stash = new List<SavedItem>();
        public List<SavedItem> run_bag = new List<SavedItem>();
        public List<SavedItem> equipped = new List<SavedItem>();
        public bool has_basic_loadout;
        public List<string> unequipped_basic_slots = new List<string>();
        public SaveSettings settings = new SaveSettings();
        public bool expedition_in_progress;
        public int gold;
    }

    [Serializable]
    private sealed class SavedItem
    {
        public string item_id;
        public string instance_id;
    }

    [Serializable]
    private sealed class SaveSettings
    {
        public bool fullscreen;
        public int resolution_width;
        public int resolution_height;

        public static SaveSettings Default()
        {
            return new SaveSettings
            {
                fullscreen = Screen.fullScreen,
                resolution_width = Mathf.Max(640, Screen.width),
                resolution_height = Mathf.Max(360, Screen.height)
            };
        }

        public SaveSettings Normalized()
        {
            if (resolution_width <= 0 || resolution_height <= 0) return Default();
            return Clone();
        }

        public SaveSettings Clone()
        {
            return new SaveSettings
            {
                fullscreen = fullscreen,
                resolution_width = resolution_width,
                resolution_height = resolution_height
            };
        }
    }
}
