using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tập trung tất cả API call liên quan đến shop.
/// Sau mỗi call thành công → cập nhật DataManager local luôn.
/// </summary>
public class ShopApiService : MonoBehaviour
{
    public static ShopApiService Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    bool IsOnlineAndLoggedIn =>
        ApiClient.Instance != null && AuthManager.Instance.IsLoggedIn;

    // ── Gun ───────────────────────────────────────────────────────

    public async void PurchaseGun(string gunId, Action onSuccess, Action<string> onError)
    {
        DataManager.Instance.PurchaseGun(gunId);   // local trước

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try
        {
            await ApiClient.Instance.Post("/player/purchase/gun",
                new { itemId = gunId });
            await SyncCoinsFromServer();
            onSuccess?.Invoke();
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    public async void EquipGun(string gunId, Action onSuccess = null)
    {
        DataManager.Instance.EquipGun(gunId);

        if (!IsOnlineAndLoggedIn) return;

        try { await ApiClient.Instance.Post("/player/equip/gun", new { itemId = gunId }); }
        catch (Exception e) { Debug.LogWarning("[Shop] EquipGun lỗi: " + e.Message); }

        onSuccess?.Invoke();
    }

    // ── Character ─────────────────────────────────────────────────

    public async void PurchaseCharacter(string charId, Action onSuccess, Action<string> onError)
    {
        DataManager.Instance.PurchaseCharacter(charId);

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try
        {
            await ApiClient.Instance.Post("/player/purchase/character",
                new { itemId = charId });
            await SyncCoinsFromServer();
            onSuccess?.Invoke();
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    public async void EquipCharacter(string charId, Action onSuccess = null)
    {
        DataManager.Instance.EquipCharacter(charId);

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try { await ApiClient.Instance.Post("/player/equip/character", new { itemId = charId }); }
        catch (Exception e) { Debug.LogWarning("[Shop] EquipCharacter lỗi: " + e.Message); }

        onSuccess?.Invoke();
    }

    // ── Costume ───────────────────────────────────────────────────

    public async void PurchaseCostume(string charId, string costumeId, int price,
        Action onSuccess, Action<string> onError)
    {
        DataManager.Instance.PurchaseCostume(charId, costumeId);

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try
        {
            await ApiClient.Instance.Post("/player/purchase/costume",
                new { characterId = charId, itemId = costumeId, price });
            await SyncCoinsFromServer();
            onSuccess?.Invoke();
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    public async void EquipCostume(string charId, string costumeId, Action onSuccess = null)
    {
        DataManager.Instance.EquipCostume(charId, costumeId);

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try
        {
            await ApiClient.Instance.Post("/player/equip/costume",
                new { characterId = charId, itemId = costumeId, price = 0 });
        }
        catch (Exception e) { Debug.LogWarning("[Shop] EquipCostume lỗi: " + e.Message); }

        onSuccess?.Invoke();
    }

    // ── Upgrade ───────────────────────────────────────────────────

    public async void UpgradeStat(string charId, string statType, int cost,
        Action onSuccess, Action<string> onError)
    {
        DataManager.Instance.UpgradeStat(charId, statType);

        if (!IsOnlineAndLoggedIn) { onSuccess?.Invoke(); return; }

        try
        {
            await ApiClient.Instance.Post("/player/upgrade",
                new { characterId = charId, statType, cost });
            await SyncCoinsFromServer();
            onSuccess?.Invoke();
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    // ── Helper ────────────────────────────────────────────────────

    // Đồng bộ coins từ server về local sau mỗi giao dịch
    async Task SyncCoinsFromServer()
    {
        try
        {
            string json = await ApiClient.Instance.Get("/player/data");
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
                DataManager.Instance.LoadFromServer(data);
        }
        catch { /* không sao, local đã cập nhật rồi */ }
    }
}