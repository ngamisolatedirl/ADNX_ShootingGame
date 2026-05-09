using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attach vào Player Prefab cùng với các component khác.
/// Nhận lệnh teleport từ server và tự apply trên máy owner.
/// Vì owner tự apply → không conflict với Client Authority NetworkTransform.
/// </summary>
public class PlayerSpawnReceiver : NetworkBehaviour
{
    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        StartCoroutine(TeleportWhenGroundReady(position));
    }

    private System.Collections.IEnumerator TeleportWhenGroundReady(Vector3 position)
    {
        var rb = GetComponent<Rigidbody2D>();

        // Freeze ngay để không rơi trong lúc chờ
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // Chờ đến khi Tilemap Collider (ground) đã load xong trong scene
        Collider2D groundCollider = null;
        while (groundCollider == null)
        {
            // Tìm collider có tag Ground hoặc tên Tilemap
            // Ưu tiên tìm theo tag "Ground" trước
            var groundObj = GameObject.FindGameObjectWithTag("Ground");
            if (groundObj != null)
                groundCollider = groundObj.GetComponent<Collider2D>();

            // Fallback: tìm theo tên tilemap
            if (groundCollider == null)
            {
                var tilemapObj = GameObject.Find("Tilemap_Ground");
                if (tilemapObj != null)
                    groundCollider = tilemapObj.GetComponent<Collider2D>();
            }

            if (groundCollider == null)
                yield return null; // chờ frame tiếp theo
        }

        // Thêm 2 frame nữa để composite collider build xong
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Teleport đến spawn point
        transform.position = position;

        // Restore physics
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log($"[SpawnReceiver] Teleport xong tại {position}");
    }
} 