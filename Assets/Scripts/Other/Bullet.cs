using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Rigidbody2D rb;
    private bool hasHit = false;
    private bool piercing = false;

    // NetworkVariable để sync direction xuống tất cả client
    // Server gán trước khi Spawn → client nhận đúng giá trị trong OnNetworkSpawn
    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // damage và piercing cũng cần sync vì client cần biết để hiển thị đúng
    // (damage xử lý trên server, nhưng piercing ảnh hưởng visual)
    private NetworkVariable<bool> networkPiercing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Gọi từ server TRƯỚC khi netObj.Spawn()
    public void SetDirection(Vector2 dir)
    {
        networkDirection.Value = dir;
    }

    public void SetStats(float dmg, bool isPiercing)
    {
        damage = dmg;
        piercing = isPiercing;
        networkPiercing.Value = isPiercing;
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        // Lúc này networkDirection đã có giá trị đúng (server gán trước Spawn)
        // Cả server lẫn client đều chạy đoạn này → đạn bay đúng trên mọi máy
        if (rb != null)
        {
            rb.linearVelocity = networkDirection.Value * speed;
        }

        // Sync piercing từ NetworkVariable (server đã set trước Spawn)
        piercing = networkPiercing.Value;

        // Chỉ server đếm ngược tự hủy
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), 3f);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ server xử lý va chạm và gây sát thương
        if (!IsServer) return;
        if (hasHit && !piercing) return;

        if (collision.CompareTag("Enemy") || collision.CompareTag("MeleeEnemy") ||
            collision.CompareTag("Boss") || collision.CompareTag("ShootingEnemy"))
        {
            if (collision.TryGetComponent<MeleeEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
            }
            // TODO: thêm IEnemyDamageable interface để không cần check từng loại

            if (!piercing)
            {
                hasHit = true;
                DestroyBullet();
            }
        }
        else if (collision.CompareTag("Ground"))
        {
            hasHit = true;
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (IsServer)
        {
            if (GetComponent<NetworkObject>().IsSpawned)
                GetComponent<NetworkObject>().Despawn();
        }
    }
}