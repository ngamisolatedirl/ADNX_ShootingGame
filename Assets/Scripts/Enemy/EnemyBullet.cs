using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 10f;
    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}