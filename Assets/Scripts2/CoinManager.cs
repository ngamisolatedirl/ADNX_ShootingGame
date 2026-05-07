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
            // Offline: cộng thẳng vào SessionData
            SessionData.Instance?.AddCoins(amount);
            SessionData.Instance?.AddKill();
            Debug.Log($"[Coin] Offline +{amount} coins");
            return;
        }

        // Online: gửi cho đúng client
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { killerClientId }
            }
        };

        AwardCoinClientRpc(amount, clientRpcParams);
        Debug.Log($"[Coin] Gửi {amount} coins → Client {killerClientId}");
    }

    [ClientRpc]
    void AwardCoinClientRpc(int amount, ClientRpcParams clientRpcParams = default)
    {
        SessionData.Instance?.AddCoins(amount);
        SessionData.Instance?.AddKill();
        Debug.Log($"[Coin] Nhận {amount} coins!");
    }
}
