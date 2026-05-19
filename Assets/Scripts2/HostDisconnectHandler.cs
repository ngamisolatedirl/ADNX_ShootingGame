using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;


public class HostDisconnectHandler : NetworkBehaviour
{
    [Header("Scene to load when host disconnects")]
    public string mainMenuScene = "MainMenu";

    [Header("(Optional) Message shown to clients")]
    public string disconnectMessage = "Host đã thoát khỏi game.";



    public override void OnNetworkSpawn()
    {

        if (!IsHost)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // ── Host gọi khi muốn thoát (từ nút Quit / Escape menu, v.v.) ──────────

    public void HostQuit()
    {
        if (!IsHost) return;

        // Báo tất cả client trước
        NotifyClientsHostQuitClientRpc();

        // Dọn dẹp phía host
        DoLeave();
    }

    // ── ClientRpc: server → tất cả client ─────────────────────────────────

    [ClientRpc]
    private void NotifyClientsHostQuitClientRpc()
    {

        if (IsHost) return;

        Debug.Log("[HostDisconnect] Host đã thoát. Chuyển về main menu...");
        DoLeave();
    }

    // ── Client tự phát hiện mất kết nối (trường hợp host crash / force quit) ──

    private void OnClientDisconnected(ulong clientId)
    {
        // Với client: khi callback này fire mà IsConnectedClient == false
        // nghĩa là chính mình bị disconnect (thường do host đã shutdown server).
        if (IsHost) return;                         // host không cần xử lý
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsConnectedClient) return; // vẫn còn kết nối → không làm gì

        Debug.Log("[HostDisconnect] Mất kết nối với host. Chuyển về main menu...");
        DoLeave();
    }

    // ── Shared cleanup ─────────────────────────────────────────────────────

    private void DoLeave()
    {
        // Dọn LAN discovery nếu có
        LanDiscovery.Instance?.StopBroadcast();
        LanDiscovery.Instance?.StopListening();

        // Dọn session
        SessionData.Instance?.Reset();

        // Dọn room context
        RoomContext.Clear();

        // Shutdown network trước khi load scene
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuScene);
    }
}