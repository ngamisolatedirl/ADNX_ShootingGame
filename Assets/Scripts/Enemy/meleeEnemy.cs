using UnityEngine;
using System;

public class MeleeEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    public float damage = 15f;
    public float attackCooldown = 1f;

    [Header("Attack Settings")]
    public float attackRange = 1.2f;
    public float detectionRange = 8f;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 1.5f;

    private float currentHealth;
    private float attackTimer = 0f;
    private Transform player;
    private UIManager uiManager;
    private bool movingRight = true;

    public Action OnDeath;

    private enum State { Patrol, Chase, Attack, ReturnToZone }
    private State currentState = State.Patrol;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindWithTag("Player").transform;
        uiManager = FindFirstObjectByType<UIManager>();

        // Thêm đoạn này
        Collider2D playerCol = player.GetComponent<Collider2D>();
        Collider2D myCol = GetComponent<Collider2D>();
        if (playerCol != null && myCol != null)
            Physics2D.IgnoreCollision(myCol, playerCol);
    }

    void Update()
    {
        attackTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                CheckDetection();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.Attack:
                AttackPlayer();
                break;
            case State.ReturnToZone:
                ReturnToZone();
                break;
        }
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

    void CheckDetection()
    {
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
            currentState = State.Chase;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange)
        {
            currentState = State.ReturnToZone;
            return;
        }

        if (dist <= attackRange)
        {
            currentState = State.Attack;
            return;
        }

        MoveTowardsX(player.position.x, moveSpeed);
    }

    void AttackPlayer()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Player chạy ra xa → đuổi lại
        if (dist > attackRange * 1.3f)
        {
            currentState = State.Chase;
            return;
        }

        // Đứng yên, quay mặt về phía player
        FacePlayer();

        // Đánh khi hết cooldown
        if (attackTimer >= attackCooldown)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(damage);
            attackTimer = 0f;
        }
    }

    void ReturnToZone()
    {
        if (zoneStart == null || zoneEnd == null) return;

        float centerX = (zoneStart.position.x + zoneEnd.position.x) / 2f;
        MoveTowardsX(centerX, moveSpeed);

        bool inZone = transform.position.x >= zoneStart.position.x
                   && transform.position.x <= zoneEnd.position.x;
        if (inZone)
            currentState = State.Patrol;
    }

    void MoveTowardsX(float targetX, float speed)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) return;

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    void FacePlayer()
    {
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (uiManager != null) uiManager.AddKill();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (zoneStart != null && zoneEnd != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(zoneStart.position, zoneEnd.position);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}