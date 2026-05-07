using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Khu vực tử vong (rơi xuống hố).
/// Hoạt động cả offline lẫn online.
/// </summary>
public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var ph = collision.GetComponent<PlayerHealth>();
        if (ph == null) return;

        // Online: chỉ xử lý trên owner
        if (NetworkUtils.IsOnline)
        {
            var nb = collision.GetComponent<NetworkBehaviour>();
            if (nb == null || !nb.IsOwner) return;
        }

        ph.Die();
    }
}
