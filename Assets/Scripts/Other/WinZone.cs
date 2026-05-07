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
    public GameObject glowEffect;           // hiệu ứng khi có player đứng trong zone

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var ph = collision.GetComponent<PlayerHealth>();
        if (ph == null || ph.IsDead) return;

        if (!NetworkUtils.IsOnline)
        {
            // Offline: win ngay
            GameManager.Instance?.WinLevel();
            return;
        }

        // Online: chỉ owner mới gửi lên server
        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb == null || !nb.IsOwner) return;

        EnterWinZoneServerRpc(nb.OwnerClientId);

        // Local feedback: freeze player tại chỗ
        var move = collision.GetComponent<MovePlayer>();
        if (move != null) move.enabled = false;

        glowEffect?.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Nếu player rời WinZone (chưa win) → cho di chuyển lại
        var nb = collision.GetComponent<NetworkBehaviour>();
        if (nb != null && nb.IsOwner)
        {
            var move = collision.GetComponent<MovePlayer>();
            if (move != null) move.enabled = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void EnterWinZoneServerRpc(ulong clientId)
    {
        GameManager.Instance?.ReportPlayerInWinZone(clientId);
    }
}
