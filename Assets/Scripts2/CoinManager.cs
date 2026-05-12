using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Host quản lý coin drop từ enemy.
/// - Host tính toán số coin và ai nhận.
/// - Gửi TargetRpc cho đúng client đó cộng vào SessionData.
/// - Offline: cộng trực tiếp vào SessionData.
/// </summary>
public class CoinManager : NetworkBehaviour
{
    public static CoinManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Gọi từ enemy khi chết. killerClientId = client last hit.
    /// Nếu offline thì killerClientId bị bỏ qua.
    /// </summary>
    public void AwardCoin(int amount, ulong killerClientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;

        if (!NetworkUtils.IsOnline)
        {
            SessionData.Instance?.AddCoins(amount);
            SessionData.Instance?.AddKill();
            Debug.Log($"[Coin] Offline +{amount} coins");
            return;
        }

        // Online: gửi ClientRpc cho client cộng vào SessionData local
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { killerClientId }
            }
        };
        AwardCoinClientRpc(amount, clientRpcParams);

        // Nếu đã login → gửi thẳng lên server API luôn
        // Host biết killerClientId → tra SessionRegistry → lấy userId
        if (SessionRegistry.Instance != null)
        {
            int userId = SessionRegistry.Instance.GetUserId(killerClientId);
            if (userId > 0)
                SendCoinToApi(userId, amount);
        }
    }

    async void SendCoinToApi(int userId, int amount)
    {
        try
        {
            // Tạo request với userId cụ thể
            // Cần thêm endpoint /player/coins trên server
            var body = new CoinAwardRequest { userId = userId, coinsToAdd = amount };
            await ApiClient.Instance.Post("/player/coins", body);
            Debug.Log($"[Coin] Gửi {amount} coins → userId {userId} thành công");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Coin] Gửi coins API thất bại: " + e.Message);
        }
    }

    [ClientRpc]
    void AwardCoinClientRpc(int amount, ClientRpcParams clientRpcParams = default)
    {
        SessionData.Instance?.AddCoins(amount);
        SessionData.Instance?.AddKill();
        Debug.Log($"[Coin] Nhận {amount} coins!");
    }

    [System.Serializable]
    class CoinAwardRequest { public int userId; public int coinsToAdd; }
}
