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
}