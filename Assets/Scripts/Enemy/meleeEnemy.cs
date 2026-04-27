using UnityEngine;
using System;

public class MeleeEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    public float damage = 15f;
    public float attackCooldown = 1f;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 1.5f;

    [Header("Detection & Attack")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;

    [Header("Behaviour")]
    public bool isStationary = false;

    private float currentHealth;
    private float attackTimer = 0f;
    private bool movingRight = true;
    private Transform player;
    private UIManager uiManager;
    private Animator animator;
    private Vector3 startPosition;

    public Action OnDeath;

    private enum State { Patrol, Chase, Attack, ReturnToZone }
    private State currentState = State.Patrol;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindWithTag("Player").transform;
        uiManager = FindFirstObjectByType<UIManager>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        Collider2D myCol = GetComponent<Collider2D>();
        if (playerCol != null && myCol != null)
            Physics2D.IgnoreCollision(myCol, playerCol);
    }

    void Update()
    {
        attackTimer += Time.deltaTime;

        if (isStationary)
        {
            if (currentState != State.Attack)
                CheckDetection();
            else
                AttackPlayer();
            return;
        }

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

        UpdateAnimation();
    }

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;

        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;

        if (Mathf.Abs(diff) < 0.05f)
        {
            movingRight = !movingRight;
            return;
        }

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * patrolSpeed * Time.deltaTime, 0, 0);
        FlipSprite(dirX);
    }

    void CheckDetection()
    {
        if (player == null) return;
        if (currentState != State.Patrol) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= detectionRange)
            currentState = isStationary ? State.Attack : State.Chase;
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

        if (!isStationary)
        {
            if (dist > attackRange * 1.3f) { currentState = State.Chase; return; }
            if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        }
        else
        {
            // Stationary: player ra ngoài detection → về patrol
            if (dist > detectionRange) { currentState = State.Patrol; return; }
        }

        FacePlayer();

        if (attackTimer >= attackCooldown)
        {
            if (animator != null)
                animator.SetTrigger("Attack");

            player.GetComponent<PlayerHealth>().TakeDamage(damage);
            attackTimer = 0f;
        }
    }

    void ReturnToZone()
    {
        if (zoneStart == null || zoneEnd == null)
        {
            // isStationary → về vị trí ban đầu
            MoveTowardsX(startPosition.x, moveSpeed);
            float diff = Mathf.Abs(startPosition.x - transform.position.x);
            if (diff < 0.05f)
                currentState = State.Patrol;
            return;
        }

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
        FlipSprite(dirX);
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = currentState == State.Patrol ||
                        currentState == State.Chase ||
                        currentState == State.ReturnToZone;

        animator.SetBool("isWalking", isMoving);
        animator.SetBool("isAttacking", currentState == State.Attack);
    }

    void FlipSprite(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    void FacePlayer()
    {
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        FlipSprite(dirX);
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

        if (animator != null)
            animator.SetBool("isDead", true);

        Destroy(gameObject, 1f);
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