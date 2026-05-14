using UnityEngine;
using Unity.Netcode;

public class EnemyBullet : NetworkBehaviour
{
    public float speed = 8f;
    public float damage = 10f;

    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void SetDirection(Vector2 dir)
    {
        if (NetworkUtils.IsOnline)
            networkDirection.Value = dir;
        else
            GetComponent<Rigidbody2D>().linearVelocity = dir * speed;
    }

    public override void OnNetworkSpawn()
    {
        GetComponent<Rigidbody2D>().linearVelocity = networkDirection.Value * speed;

        if (IsServer)
            Invoke(nameof(DestroyBullet), 3f);
    }

    void Start()
    {
        // Offline only
        if (!NetworkUtils.IsOnline)
            Invoke(nameof(DestroyBulletOffline), 3f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Online: chỉ server xử lý
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

    void DestroyBulletOffline()
    {
        Destroy(gameObject);
    }
}