using Unity.Netcode;
using UnityEngine;

public static class NetworkUtils
{
    /// <summary>
    /// True nếu NetworkManager đang chạy (host hoặc client đã kết nối).
    /// Dùng để phân biệt online vs offline mà không cần flag thủ công.
    /// </summary>
    public static bool IsOnline =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public static bool IsHost =>
        IsOnline && NetworkManager.Singleton.IsHost;

    public static bool IsClient =>
        IsOnline && NetworkManager.Singleton.IsClient;

    /// <summary>
    /// Offline hoặc online đều là "server authority" nếu là host hoặc chơi solo.
    /// </summary>
    public static bool HasServerAuthority =>
        !IsOnline || NetworkManager.Singleton.IsServer;
}
