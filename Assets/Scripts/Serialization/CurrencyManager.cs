using UnityEngine;
using System.IO;

public class CurrencyManager : MonoBehaviour
{
    private static CurrencyManager instance;
    public static CurrencyManager Instance => instance;

    private CurrencyData data;
    private string savePath;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/currency.json";
        LoadData();
    }

    // ── Load ──────────────────────────────────────────
    void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<CurrencyData>(json);
            Debug.Log("Loaded coins: " + data.coins);
        }
        else
        {
            data = new CurrencyData { coins = 0 };
            SaveData();
        }
    }

    // ── Save ──────────────────────────────────────────
    void SaveData()
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    // ── API ───────────────────────────────────────────
    public void AddCoins(int amount)
    {
        data.coins += amount;
        SaveData();
        Debug.Log("Coins: " + data.coins);
    }

    public bool SpendCoins(int amount)
    {
        if (data.coins < amount) return false;
        data.coins -= amount;
        SaveData();
        return true;
    }

    public int GetCoins() => data.coins;
}