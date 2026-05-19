using Unity.Netcode;
using UnityEngine;

public static class NetworkUtils
{

    public static bool IsOnline =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public static bool IsHost =>
        IsOnline && NetworkManager.Singleton.IsHost;

    public static bool IsClient =>
        IsOnline && NetworkManager.Singleton.IsClient;

    public static bool HasServerAuthority =>
        !IsOnline || NetworkManager.Singleton.IsServer;
}
