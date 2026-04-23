using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 3f;
    public float damage = 10f;
    public float attackCooldown = 1f;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 2f;

    private float currentHealth;
    private float attackTimer = 0f;
    private Transform player;
    private UIManager uiManager;
    private bool isTouchingPlayer = false;

    private enum State { Patrol, Chase, ReturnToZone }
    private State currentState = State.Patrol;
    private bool movingRight = true;

    public System.Action OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindWithTag("Player").transform;
        uiManager = FindFirstObjectByType<UIManager>();

        // Không đẩy nhau
        Collider2D playerCol = player.GetComponent<Collider2D>();
        Collider2D enemyCol = GetComponent<Collider2D>();
        if (playerCol != null && enemyCol != null)
            Physics2D.IgnoreCollision(enemyCol, playerCol);
    }

    void Update()
    {
        attackTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                CheckPlayerInZone();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.ReturnToZone:
                ReturnToZone();
                break;
        }

        // Đánh khi đang chạm player
        if (isTouchingPlayer && attackTimer >= attackCooldown)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(damage);
            attackTimer = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            isTouchingPlayer = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            isTouchingPlayer = false;
    }

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;

        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        MoveTowardsX(targetX, patrolSpeed);

        if (movingRight && transform.position.x >= zoneEnd.position.x)
            movingRight = false;
        else if (!movingRight && transform.position.x <= zoneStart.position.x)
            movingRight = true;
    }

    void CheckPlayerInZone()
    {
        if (player == null) return;

        bool playerInZone = player.position.x >= zoneStart.position.x
                         && player.position.x <= zoneEnd.position.x;
        if (playerInZone)
            currentState = State.Chase;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        bool playerInZone = player.position.x >= zoneStart.position.x
                         && player.position.x <= zoneEnd.position.x;

        if (!playerInZone)
        {
            currentState = State.ReturnToZone;
            return;
        }

        MoveTowardsX(player.position.x, moveSpeed);
    }

    void ReturnToZone()
    {
        float clampedX = Mathf.Clamp(transform.position.x,
                                     zoneStart.position.x,
                                     zoneEnd.position.x);
        MoveTowardsX(clampedX, moveSpeed);

        bool inZone = transform.position.x >= zoneStart.position.x
                   && transform.position.x <= zoneEnd.position.x;
        if (inZone)
            currentState = State.Patrol;
    }

    void MoveTowardsX(float targetX, float speed)
    {
        float dirX = Mathf.Sign(targetX - transform.position.x);
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (uiManager != null)
            uiManager.AddKill();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (zoneStart == null || zoneEnd == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(zoneStart.position, zoneEnd.position);
    }
}