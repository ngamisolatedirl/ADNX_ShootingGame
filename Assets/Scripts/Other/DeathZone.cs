using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Fix: client dùng FallDeathServerRpc thay vì gọi Die() trực tiếp.
/// Die() yêu cầu IsOwner → DieServerRpc, cách này tương đương nhưng rõ hơn.
/// </summary>
public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var ph = collision.GetComponent<PlayerHealth>();
        if (ph == null) return;

        if (NetworkUtils.IsOnline)
        {
            // Chỉ xử lý trên owner của player đó
            var nb = collision.GetComponent<NetworkBehaviour>();
            if (nb == null || !nb.IsOwner) return;

            // Owner gửi ServerRpc để server xử lý death
            ph.FallDeathServerRpc();
        }
        else
        {
            ph.Die();
        }
    }
}