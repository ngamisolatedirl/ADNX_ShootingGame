using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Khu vực chiến thắng.
/// - Online: gửi lên server, server kiểm tra khi đủ tất cả player còn sống.
/// - Offline: gọi GameManager.WinLevel() trực tiếp.
/// Player đã chết không bị tính vào điều kiện.
/// </summary>
public class WinZone : NetworkBehaviour
{
    [Header("Visual Feedback")]
    public GameObject glowEffect;

    // Đếm số player đang đứng trong zone (chỉ dùng để toggle glow)
    private int playersInZone = 0;

    public override void OnNetworkSpawn()
    {
        glowEffect?.SetActive(false);
        playersInZone = 0;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var ph = collision.GetComponent<PlayerHealth>();
        if (ph == null || ph.IsDead) return;

        if (!NetworkUtils.IsOnline)
        {
            GameManager.Instance?.WinLevel();
            return;
        }

        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb == null || !nb.IsOwner) return;

        // Chỉ báo server — KHÔNG freeze ở đây
        // Server sẽ freeze tất cả qua ClientRpc khi đủ điều kiện win
        EnterWinZoneServerRpc(nb.OwnerClientId);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (!NetworkUtils.IsOnline) return;

        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb == null || !nb.IsOwner) return;

        ExitWinZoneServerRpc(nb.OwnerClientId);
    }

    // ── ServerRpc ──────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    void EnterWinZoneServerRpc(ulong clientId)
    {
        bool allIn = GameManager.Instance?.ReportPlayerInWinZone(clientId) ?? false;

        // Cập nhật glow cho tất cả client
        UpdateGlowClientRpc(true);

        // Chỉ freeze khi đủ điều kiện win
        if (allIn)
            FreezeAllPlayersClientRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    void ExitWinZoneServerRpc(ulong clientId)
    {
        GameManager.Instance?.ReportPlayerLeftWinZone(clientId);
        UpdateGlowClientRpc(false);
    }

    // ── ClientRpc ──────────────────────────────────────────────────────────

    /// <summary>
    /// Server gọi để freeze/unfreeze tất cả player khi win.
    /// Mỗi client tự tìm player của mình (IsOwner) để xử lý.
    /// </summary>
    [ClientRpc]
    void FreezeAllPlayersClientRpc(bool freeze)
    {
        // Mỗi client chỉ freeze player của chính mình
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var nb = p.GetComponent<NetworkBehaviour>();
            if (nb == null || !nb.IsOwner) continue;

            var move = p.GetComponent<MovePlayer>();
            if (move != null) move.enabled = !freeze;
        }
    }

    /// <summary>Sync glow effect cho tất cả client.</summary>
    [ClientRpc]
    void UpdateGlowClientRpc(bool active)
    {
        glowEffect?.SetActive(active);
    }
}