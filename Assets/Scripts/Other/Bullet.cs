using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasHit = false;
    private bool piercing = false;

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    public void SetStats(float dmg, bool isPiercing)
    {
        damage = dmg;
        piercing = isPiercing;
    }

    // 1. Dùng cho chế độ ONLINE
    public override void OnNetworkSpawn()
    {
        InitBullet();
    }

    // 2. Dùng cho chế độ OFFLINE
    private void Start()
    {
        // Nếu không có kết nối mạng, NetworkManager sẽ không gọi OnNetworkSpawn
        // nên ta gọi thủ công ở Start
        if (!NetworkUtils.IsOnline)
        {
            InitBullet();
        }
    }

    void InitBullet()
    {
        rb = GetComponent<Rigidbody2D>();

        // Gán vận tốc để đạn bay
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Tự hủy sau 3 giây để tránh rác bộ nhớ
        if (NetworkUtils.IsOnline)
        {
            if (IsServer) Invoke(nameof(DestroyBullet), 3f);
        }
        else
        {
            Destroy(gameObject, 3f);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // CHÚ Ý: Chỉnh sửa logic check để chạy được cả Offline
        if (NetworkUtils.IsOnline && !IsServer) return;

        if (hasHit && !piercing) return;

        // Xử lý va chạm với các loại Enemy
        if (collision.CompareTag("Enemy") || collision.CompareTag("Enemy2") ||
            collision.CompareTag("MeleeEnemy") || collision.CompareTag("Boss") ||
            collision.CompareTag("LavaEnemy") || collision.CompareTag("ShootingEnemy"))
        {
            // Thử lấy component và gây sát thương
            if (collision.TryGetComponent<MeleeEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
            }
            // Bạn có thể thêm TryGetComponent cho các class enemy khác ở đây tương tự MeleeEnemy

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
        if (NetworkUtils.IsOnline)
        {
            if (IsServer) GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}