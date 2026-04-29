using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasHit = false; // Chống trúng nhiều lần
    private bool piercing = false;

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


    

    public void SetStats(float dmg, bool isPiercing)
    {
        damage = dmg;
        piercing = isPiercing;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit && !piercing) return;

        if (collision.CompareTag("Enemy"))
        {
            collision.GetComponent<Enemy>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("Enemy2"))
        {
            collision.GetComponent<Enemy2>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("MeleeEnemy"))
        {
            collision.GetComponent<MeleeEnemy>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("Boss"))
        {
            collision.GetComponent<BossEnemy>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("LavaEnemy"))
        {
            collision.GetComponent<LavaEnemy>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("ShootingEnemy"))
        {
            collision.GetComponent<ShootingEnemy>().TakeDamage(damage);
            if (!piercing) { hasHit = true; Destroy(gameObject); }
        }
        else if (collision.CompareTag("Ground"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}