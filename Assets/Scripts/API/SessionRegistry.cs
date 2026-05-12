using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Chạy trên Host, lưu mapping clientId → userId
/// Khi player join room → gửi userId lên Host qua ServerRpc
/// </summary>
public class SessionRegistry : NetworkBehaviour
{
    public static SessionRegistry Instance { get; private set; }

    // clientId → userId
    private Dictionary<ulong, int> clientToUser = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Mỗi client tự gửi userId của mình lên host khi spawn
        if (NetworkUtils.IsOnline)
        {
            int myUserId = AuthManager.Instance?.IsLoggedIn == true
                ? AuthManager.UserId
                : 0;

            RegisterUserServerRpc(NetworkManager.Singleton.LocalClientId, myUserId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RegisterUserServerRpc(ulong clientId, int userId)
    {
        clientToUser[clientId] = userId;
        Debug.Log($"[SessionRegistry] Client {clientId} → UserId {userId}");
    }

    // Host gọi để lấy userId từ clientId
    public int GetUserId(ulong clientId)
    {
        return clientToUser.TryGetValue(clientId, out int userId) ? userId : 0;
    }
}