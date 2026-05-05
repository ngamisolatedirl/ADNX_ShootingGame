using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance => instance;

    private GunConfig gunConfig;
    private CharacterConfig characterConfig;
    private SaveData saveData;

    private string savePath;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/save_data.json";

        
        Debug.Log("Save path: " + Application.persistentDataPath);
        Init();
    }

    void Init()
    {
        LoadGunConfig();
        LoadCharacterConfig();
        LoadSaveData();
        Debug.Log("DataManager Init xong");
    }

    // ── Load Config ───────────────────────────────

    void LoadGunConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "gun_config.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            gunConfig = JsonUtility.FromJson<GunConfig>(json);
            Debug.Log("Loaded " + gunConfig.guns.Count + " guns");
        }
        else
            Debug.LogError("Không tìm thấy gun_config.json!");
    }

    void LoadCharacterConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "character_config.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            characterConfig = JsonUtility.FromJson<CharacterConfig>(json);
            Debug.Log("Loaded " + characterConfig.characters.Count + " characters");
        }
        else
            Debug.LogError("Không tìm thấy character_config.json!");
    }

    void LoadSaveData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Loaded save data, coins: " + saveData.coins);
        }
        else
        {
            // Tạo save data mặc định
            CreateDefaultSaveData();
            SaveGame();
        }
    }

    void CreateDefaultSaveData()
    {
        saveData = new SaveData
        {
            coins = 0,
            activeCharacterId = "cowboy",
            activeGunId = "pistol",
            purchasedGuns = new System.Collections.Generic.List<string> { "pistol" },
            characters = new System.Collections.Generic.List<CharacterSaveData>
            {
                new CharacterSaveData
                {
                    id = "cowboy",
                    isPurchased = true,
                    activeCostumeId = "cowboy_default",
                    purchasedCostumes = new System.Collections.Generic.List<string> { "cowboy_default" },
                    upgradeLevels = new UpgradeLevels { hp = 0, speed = 0, critRate = 0, critDamage = 0 }
                },
                new CharacterSaveData
                {
                    id = "ranger",
                    isPurchased = false,
                    activeCostumeId = "ranger_default",
                    purchasedCostumes = new System.Collections.Generic.List<string> { "ranger_default" },
                    upgradeLevels = new UpgradeLevels { hp = 0, speed = 0, critRate = 0, critDamage = 0 }
                }
            }
        };
        Debug.Log("Tạo save data mặc định");
    }

    // ── Save ──────────────────────────────────────

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Saved game");
    }

    // ── Getter API ────────────────────────────────

    public GunData GetGunData(string id)
        => gunConfig.guns.Find(g => g.id == id);

    public GunData GetActiveGun()
        => GetGunData(saveData.activeGunId);

    public CharacterData GetCharacterData(string id)
        => characterConfig.characters.Find(c => c.id == id);

    public CharacterData GetActiveCharacter()
        => GetCharacterData(saveData.activeCharacterId);

    public CharacterSaveData GetCharacterSaveData(string id)
        => saveData.characters.Find(c => c.id == id);

    public CharacterSaveData GetActiveCharacterSaveData()
        => GetCharacterSaveData(saveData.activeCharacterId);

    public SaveData GetSaveData() => saveData;
    public GunConfig GetGunConfig() => gunConfig;
    public CharacterConfig GetCharacterConfig() => characterConfig;

    // ── Computed Stats ────────────────────────────

    public BaseStats GetComputedStats(string characterId)
    {
        CharacterData config = GetCharacterData(characterId);
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (config == null || save == null) return null;

        return new BaseStats
        {
            hp = config.baseStats.hp + save.upgradeLevels.hp * config.upgrades.hp.increasePerLevel,
            speed = config.baseStats.speed + save.upgradeLevels.speed * config.upgrades.speed.increasePerLevel,
            critRate = config.baseStats.critRate + save.upgradeLevels.critRate * config.upgrades.critRate.increasePerLevel,
            critDamage = config.baseStats.critDamage + save.upgradeLevels.critDamage * config.upgrades.critDamage.increasePerLevel
        };
    }

    // ── Currency ──────────────────────────────────

    public void AddCoins(int amount)
    {
        saveData.coins += amount;
        SaveGame();
    }

    public bool SpendCoins(int amount)
    {
        if (saveData.coins < amount) return false;
        saveData.coins -= amount;
        SaveGame();
        return true;
    }

    public int GetCoins() => saveData.coins;

    public void PurchaseGun(string id)
    {
        if (saveData.purchasedGuns.Contains(id)) return;
        saveData.purchasedGuns.Add(id);
        SaveGame();
    }

    public void EquipGun(string id)
    {
        saveData.activeGunId = id;
        SaveGame();
    }

    public bool IsGunPurchased(string id)
        => saveData.purchasedGuns.Contains(id);

    public bool IsGunActive(string id)
        => saveData.activeGunId == id;

    public void PurchaseCharacter(string id)
    {
        CharacterSaveData save = GetCharacterSaveData(id);
        if (save == null) return;
        save.isPurchased = true;
        SaveGame();
    }

    public void EquipCharacter(string id)
    {
        saveData.activeCharacterId = id;
        SaveGame();
    }

    public bool IsCharacterPurchased(string id)
        => GetCharacterSaveData(id)?.isPurchased ?? false;

    public bool IsCharacterActive(string id)
        => saveData.activeCharacterId == id;

    // STATS UPGRADES
    public bool UpgradeStat(string characterId, string statType)
    {
        CharacterData config = GetCharacterData(characterId);
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (config == null || save == null) return false;

        UpgradeStat stat;
        int currentLevel;

        switch (statType)
        {
            case "hp":
                stat = config.upgrades.hp;
                currentLevel = save.upgradeLevels.hp;
                break;
            case "speed":
                stat = config.upgrades.speed;
                currentLevel = save.upgradeLevels.speed;
                break;
            case "critRate":
                stat = config.upgrades.critRate;
                currentLevel = save.upgradeLevels.critRate;
                break;
            case "critDamage":
                stat = config.upgrades.critDamage;
                currentLevel = save.upgradeLevels.critDamage;
                break;
            default: return false;
        }

        // Check max level
        if (currentLevel >= stat.maxLevel) return false;

        // Check coins
        int cost = stat.costPerLevel[currentLevel];
        if (!SpendCoins(cost)) return false;

        // Tăng level
        switch (statType)
        {
            case "hp": save.upgradeLevels.hp++; break;
            case "speed": save.upgradeLevels.speed++; break;
            case "critRate": save.upgradeLevels.critRate++; break;
            case "critDamage": save.upgradeLevels.critDamage++; break;
        }

        SaveGame();
        return true;
    }

    public int GetUpgradeCost(string characterId, string statType)
    {
        CharacterData config = GetCharacterData(characterId);
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (config == null || save == null) return -1;

        switch (statType)
        {
            case "hp": return save.upgradeLevels.hp >= config.upgrades.hp.maxLevel ? -1 : config.upgrades.hp.costPerLevel[save.upgradeLevels.hp];
            case "speed": return save.upgradeLevels.speed >= config.upgrades.speed.maxLevel ? -1 : config.upgrades.speed.costPerLevel[save.upgradeLevels.speed];
            case "critRate": return save.upgradeLevels.critRate >= config.upgrades.critRate.maxLevel ? -1 : config.upgrades.critRate.costPerLevel[save.upgradeLevels.critRate];
            case "critDamage": return save.upgradeLevels.critDamage >= config.upgrades.critDamage.maxLevel ? -1 : config.upgrades.critDamage.costPerLevel[save.upgradeLevels.critDamage];
            default: return -1;
        }
    }

    public int GetUpgradeLevel(string characterId, string statType)
    {
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (save == null) return 0;

        switch (statType)
        {
            case "hp": return save.upgradeLevels.hp;
            case "speed": return save.upgradeLevels.speed;
            case "critRate": return save.upgradeLevels.critRate;
            case "critDamage": return save.upgradeLevels.critDamage;
            default: return 0;
        }
    }

    public void EquipCostume(string characterId, string costumeId)
    {
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (save == null) return;
        save.activeCostumeId = costumeId;
        SaveGame();
    }

    public void PurchaseCostume(string characterId, string costumeId)
    {
        CharacterSaveData save = GetCharacterSaveData(characterId);
        if (save == null) return;
        if (save.purchasedCostumes.Contains(costumeId)) return;
        save.purchasedCostumes.Add(costumeId);
        SaveGame();
    }

    public bool IsCostumePurchased(string characterId, string costumeId)
    {
        CharacterSaveData save = GetCharacterSaveData(characterId);
        return save?.purchasedCostumes.Contains(costumeId) ?? false;
    }

    public bool IsCostumeActive(string characterId, string costumeId)
    {
        CharacterSaveData save = GetCharacterSaveData(characterId);
        return save?.activeCostumeId == costumeId;
    }

}