using UnityEngine;
using Unity.Netcode;

public class LavaProjectile : NetworkBehaviour
{
    public float speed = 5f;
    public float damage = 10f;

    public override void OnNetworkSpawn()
    {
        // Tất cả client đều set velocity khi spawn
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * speed;

        if (IsServer)
            Invoke(nameof(DestroyBullet), 5f);
    }

    void Start()
    {
        // Offline only
        if (!NetworkUtils.IsOnline)
        {
            GetComponent<Rigidbody2D>().gravityScale = 0f;
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * speed;
            Invoke(nameof(DestroyOffline), 5f);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (NetworkUtils.IsOnline && !IsServer) return;

        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            if (NetworkUtils.IsOnline)
                DestroyBullet();
            else
                Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground"))
        {
            if (NetworkUtils.IsOnline)
                DestroyBullet();
            else
                Destroy(gameObject);
        }
    }

    void DestroyBullet()
    {
        if (IsServer && GetComponent<NetworkObject>().IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }

    void DestroyOffline()
    {
        Destroy(gameObject);
    }
}