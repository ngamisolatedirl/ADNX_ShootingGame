using UnityEngine;

public class BossBullet : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 8f;

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.left * speed;
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerHealth>().TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}