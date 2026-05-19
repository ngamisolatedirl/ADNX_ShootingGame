using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;


public class HostDisconnectHandler : NetworkBehaviour
{
    [Header("Scene to load when host disconnects")]
    public string mainMenuScene = "MainMenu";

    [Header("(Optional) Message shown to clients")]
    public string disconnectMessage = "Host đã thoát khỏi game.";

    // Guard tránh DoLeave chạy 2 lần (vd: vừa bấm Leave vừa nhận RPC)
    private bool _leaving = false;

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

    // ── Host gọi khi muốn thoát ────────────────────────────────────────────

    public void HostQuit()
    {
        if (!IsHost) return;
        NotifyClientsHostQuitClientRpc();
        DoLeave();
    }

    // ── Client CHỦ ĐỘNG bấm nút thoát ─────────────────────────────────────
    // Gắn hàm này vào nút Back/Quit phía client thay vì gọi LoadScene trực tiếp.

    public void ClientLeave()
    {
        if (IsHost) { HostQuit(); return; }
        if (_leaving) return;
        StartCoroutine(ClientLeaveRoutine());
    }

    private IEnumerator ClientLeaveRoutine()
    {
        _leaving = true;

        // Bước 1: Yêu cầu server kick mình ra TRƯỚC
        // → Netcode xóa client khỏi ConnectedClientsList ngay trên server
        // → host sẽ không gửi thêm LoadScene RPC cho client này nữa
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            NotifyServerClientLeavingServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        // Bước 2: Đợi đủ lâu để server xử lý DisconnectClient
        // (2 frame thường đủ cho 1 RTT local, dùng 0.1s để an toàn hơn)
        yield return new WaitForSecondsRealtime(1f);

        // Bước 3: Tắt hẳn phía client rồi về menu
        DoLeave();
    }

    // Server nhận yêu cầu, kick client ra khỏi ConnectedClients ngay lập tức
    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerClientLeavingServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"[HostDisconnect] Client {clientId} tự nguyện thoát, server kick.");
        if (clientId != NetworkManager.ServerClientId)
            NetworkManager.Singleton.DisconnectClient(clientId, "Người chơi tự thoát.");
    }

    // ── ClientRpc: server → tất cả client (khi host thoát) ────────────────

    [ClientRpc]
    private void NotifyClientsHostQuitClientRpc()
    {
        if (IsHost) return;
        Debug.Log("[HostDisconnect] Host đã thoát. Chuyển về main menu...");
        if (_leaving) return;   // tránh chạy 2 lần nếu client đang tự thoát
        DoLeave();
    }

    // ── Client tự phát hiện mất kết nối (host crash / force quit) ─────────

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsHost) return;
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsConnectedClient) return; // vẫn còn kết nối → bỏ qua

        Debug.Log("[HostDisconnect] Mất kết nối với host. Chuyển về main menu...");
        if (_leaving) return;   // đã tự thoát trước đó → không cần làm lại
        DoLeave();
    }

    // ── Shared cleanup ──────────────────────────────────────────────────────

    private void DoLeave()
    {
        _leaving = true;

        LanDiscovery.Instance?.StopBroadcast();
        LanDiscovery.Instance?.StopListening();

        SessionData.Instance?.Reset();
        RoomContext.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuScene);
    }
}