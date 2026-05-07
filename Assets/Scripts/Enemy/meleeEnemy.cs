using System;
using UnityEngine;
using Unity.Netcode;

public class MeleeEnemy : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    public float damage = 15f;
    public float attackCooldown = 1f;
    public int coinDrop = 10;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 1.5f;

    [Header("Detection & Attack")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;

    [Header("Behaviour")]
    public bool isStationary = false;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float attackTimer = 0f;
    private bool movingRight = true;
    private Transform player;
    private UIManager uiManager;
    private Animator animator;
    private Vector3 startPosition;
    private bool isDead = false;

    // Track last attacker để award coin
    private ulong lastAttackerClientId = 0;

    public Action OnDeath;

    private enum State { Patrol, Chase, Attack, ReturnToZone }
    private State currentState = State.Patrol;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            player = GameObject.FindWithTag("Player")?.transform;
            uiManager = FindFirstObjectByType<UIManager>();
            startPosition = transform.position;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (player != null)
            {
                Collider2D playerCol = player.GetComponent<Collider2D>();
                Collider2D myCol = GetComponent<Collider2D>();
                if (playerCol != null && myCol != null)
                    Physics2D.IgnoreCollision(myCol, playerCol);
            }
        }
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsServer) return;
        if (player == null) return;

        attackTimer += Time.deltaTime;

        if (isStationary)
        {
            if (currentState != State.Attack) CheckDetection();
            else AttackPlayer();
            return;
        }

        switch (currentState)
        {
            case State.Patrol: Patrol(); CheckDetection(); break;
            case State.Chase: ChasePlayer(); break;
            case State.Attack: AttackPlayer(); break;
            case State.ReturnToZone: ReturnToZone(); break;
        }

        UpdateAnimation();
    }

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) { movingRight = !movingRight; return; }
        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * patrolSpeed * Time.deltaTime, 0, 0);
        FlipSprite(dirX);
    }

    void CheckDetection()
    {
        if (player == null || currentState != State.Patrol) return;
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
            currentState = isStationary ? State.Attack : State.Chase;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        if (dist <= attackRange) { currentState = State.Attack; return; }
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
            if (dist > detectionRange) { currentState = State.Patrol; return; }
        }

        FacePlayer();

        if (attackTimer >= attackCooldown)
        {
            animator?.SetTrigger("Attack");
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            attackTimer = 0f;
        }
    }

    void ReturnToZone()
    {
        if (zoneStart == null || zoneEnd == null)
        {
            MoveTowardsX(startPosition.x, moveSpeed);
            if (Mathf.Abs(startPosition.x - transform.position.x) < 0.05f)
                currentState = State.Patrol;
            return;
        }

        float centerX = (zoneStart.position.x + zoneEnd.position.x) / 2f;
        MoveTowardsX(centerX, moveSpeed);
        bool inZone = transform.position.x >= zoneStart.position.x
                   && transform.position.x <= zoneEnd.position.x;
        if (inZone) currentState = State.Patrol;
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
        bool isMoving = currentState == State.Patrol || currentState == State.Chase || currentState == State.ReturnToZone;
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
        FlipSprite(Mathf.Sign(player.position.x - transform.position.x));
    }

    // ── Damage / Death ─────────────────────────────────────────────────────

    public void TakeDamage(float dmg, ulong attackerClientId = 0)
    {
        if (!IsServer || isDead) return;

        lastAttackerClientId = attackerClientId;
        currentHealth.Value -= dmg;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        if (currentHealth.Value <= 0) Die();
    }

    // Backward compat: gọi không có attackerId (từ Bullet cũ)
    public void TakeDamage(float dmg) => TakeDamage(dmg, 0);

    void Die()
    {
        if (isDead) return;
        isDead = true;

        uiManager?.AddKill();

        // Dùng CoinManager thay vì DataManager trực tiếp
        CoinManager.Instance?.AwardCoin(coinDrop, lastAttackerClientId);

        OnDeath?.Invoke();
        DieClientRpc();
        Invoke(nameof(DespawnEnemy), 0.2f);
    }

    [ClientRpc]
    void DieClientRpc()
    {
        animator?.SetBool("isDead", true);
    }

    void DespawnEnemy()
    {
        GetComponent<NetworkObject>().Despawn();
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