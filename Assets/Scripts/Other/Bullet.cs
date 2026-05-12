using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Rigidbody2D rb;
    private bool hasHit = false;
    private bool piercing = false;

    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> networkPiercing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Owner của viên đạn (client nào bắn ra)
    private NetworkVariable<ulong> networkOwnerClientId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

    public void SetOwner(ulong clientId)
    {
        networkOwnerClientId.Value = clientId;
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = networkDirection.Value * speed;

        piercing = networkPiercing.Value;

        if (IsServer)
            Invoke(nameof(DestroyBullet), 3f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (hasHit && !piercing) return;

        if (collision.CompareTag("Enemy") || collision.CompareTag("MeleeEnemy") ||
            collision.CompareTag("Boss") || collision.CompareTag("ShootingEnemy"))
        {
            if (collision.TryGetComponent<MeleeEnemy>(out var enemy))
            {
                // Truyền ownerClientId → enemy biết ai bắn trúng → coin về đúng người
                enemy.TakeDamage(damage, networkOwnerClientId.Value);
            }

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
        if (IsServer && GetComponent<NetworkObject>().IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }
}