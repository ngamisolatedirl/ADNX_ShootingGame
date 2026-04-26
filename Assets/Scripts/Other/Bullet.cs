using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasHit = false; // Chống trúng nhiều lần

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return; // Đã trúng rồi thì bỏ qua

        if (collision.CompareTag("Enemy"))
        {
            hasHit = true;
            collision.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Enemy2"))
        {
            hasHit = true;
            collision.GetComponent<Enemy2>().TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("MeleeEnemy"))
        {
            hasHit = true;
            collision.GetComponent<MeleeEnemy>().TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Boss"))
        {
            hasHit = true;
            collision.GetComponent<BossEnemy>().TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}