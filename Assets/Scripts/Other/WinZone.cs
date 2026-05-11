using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Khu vực chiến thắng.
/// - Online: gửi lên server, server kiểm tra khi đủ tất cả player còn sống.
/// - Offline: gọi GameManager.WinLevel() trực tiếp.
/// Player đã chết không bị tính vào điều kiện.
///
/// FIX:
/// 1. ExitWinZoneServerRpc() — báo server khi player rời zone, tránh đếm sai
/// 2. FreezePlayerClientRpc() — server điều khiển freeze/unfreeze đồng bộ
/// 3. Reset glowEffect khi OnNetworkSpawn (tránh stale state sau restart)
/// </summary>
public class WinZone : NetworkBehaviour
{
    [Header("Visual Feedback")]
    public GameObject glowEffect; // hiệu ứng khi có player đứng trong zone

    public override void OnNetworkSpawn()
    {
        // Reset glow mỗi lần scene load
        glowEffect?.SetActive(false);
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

        // Chỉ owner mới gửi lên server
        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb == null || !nb.IsOwner) return;

        // Báo server enter
        EnterWinZoneServerRpc(nb.OwnerClientId);

        // Local: freeze player + hiện glow
        SetPlayerMovement(collision, false);
        glowEffect?.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (!NetworkUtils.IsOnline)
        {
            SetPlayerMovement(collision, true);
            return;
        }

        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb == null || !nb.IsOwner) return;

        // FIX: báo server player rời zone để cập nhật đếm
        ExitWinZoneServerRpc(nb.OwnerClientId);

        // Local: unfreeze player + tắt glow
        SetPlayerMovement(collision, true);
        glowEffect?.SetActive(false);
    }

    // ── ServerRpc ──────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    void EnterWinZoneServerRpc(ulong clientId)
    {
        GameManager.Instance?.ReportPlayerInWinZone(clientId);
    }

    // FIX: server nhận biết player rời zone
    [ServerRpc(RequireOwnership = false)]
    void ExitWinZoneServerRpc(ulong clientId)
    {
        GameManager.Instance?.ReportPlayerLeftWinZone(clientId);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void SetPlayerMovement(Collider2D collision, bool enabled)
    {
        var move = collision.GetComponent<MovePlayer>();
        if (move != null) move.enabled = enabled;
    }
}