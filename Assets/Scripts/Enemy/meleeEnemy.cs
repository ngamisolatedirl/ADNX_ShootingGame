using System;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
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

    // --- Health Management ---
    private float localHealth; // Dùng cho Offline
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float attackTimer = 0f;
    private bool movingRight = true;
    private Transform player;
    private UIManager uiManager;
    private Animator animator;
    private Vector3 startPosition;
    private bool isDead = false;
    private ulong lastAttackerClientId = 0;

    public Action OnDeath;

    private enum State { Patrol, Chase, Attack, ReturnToZone }
    private State currentState = State.Patrol;

    private void Start()
    {
        // Khởi tạo các thành phần cơ bản (Luôn chạy dù Online hay Offline)
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        localHealth = maxHealth;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Nếu chơi Offline, tìm Player ngay lập tức
        if (!NetworkUtils.IsOnline)
        {
            FindTargetPlayer();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Chỉ chạy khi có kết nối mạng (Host/Server)
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            FindTargetPlayer();
        }
    }

    private void FindTargetPlayer()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        uiManager = FindFirstObjectByType<UIManager>();

        if (player != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            Collider2D myCol = GetComponent<Collider2D>();
            if (playerCol != null && myCol != null)
                Physics2D.IgnoreCollision(myCol, playerCol);
        }
    }

    void Update()
    {
        // Kiểm tra quyền điều khiển: Chỉ Server hoặc người chơi Offline mới chạy logic AI
        if (!NetworkUtils.HasServerAuthority) return;
        if (isDead) return;

        if (player == null)
        {
            FindTargetPlayer();
            return;
        }

        attackTimer += Time.deltaTime;

        if (isStationary)
        {
            if (currentState != State.Attack) CheckDetection();
            else AttackPlayer();
        }
        else
        {
            switch (currentState)
            {
                case State.Patrol: Patrol(); CheckDetection(); break;
                case State.Chase: ChasePlayer(); break;
                case State.Attack: AttackPlayer(); break;
                case State.ReturnToZone: ReturnToZone(); break;
            }
        }

        UpdateAnimation();
    }

    // ── Logic Di Chuyển / AI ────────────────────────────────────────────────

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;

        if (Mathf.Abs(diff) < 0.1f) { movingRight = !movingRight; return; }

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * patrolSpeed * Time.deltaTime, 0, 0);
        FlipSprite(dirX);
    }

    void CheckDetection()
    {
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
            currentState = isStationary ? State.Attack : State.Chase;
    }

    void ChasePlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        if (dist <= attackRange) { currentState = State.Attack; return; }

        MoveTowardsX(player.position.x, moveSpeed);
    }

    void AttackPlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (!isStationary)
        {
            if (dist > attackRange * 1.2f) { currentState = State.Chase; return; }
        }
        else
        {
            if (dist > detectionRange) { currentState = State.Patrol; return; }
        }

        FacePlayer();

        if (attackTimer >= attackCooldown)
        {
            animator?.SetTrigger("Attack");
            // Gọi gây sát thương cho Player
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            attackTimer = 0f;
        }
    }

    void ReturnToZone()
    {
        float targetX = (zoneStart != null && zoneEnd != null)
            ? (zoneStart.position.x + zoneEnd.position.x) / 2f
            : startPosition.x;

        MoveTowardsX(targetX, moveSpeed);

        if (Mathf.Abs(targetX - transform.position.x) < 0.1f)
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

    // ── Hiển Thị / Animation ────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (animator == null) return;
        bool isMoving = currentState == State.Patrol || currentState == State.Chase || currentState == State.ReturnToZone;
        animator.SetBool("isWalking", isMoving);
        animator.SetBool("isAttacking", currentState == State.Attack);
    }

    void FlipSprite(float dirX)
    {
        if (dirX == 0) return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    void FacePlayer()
    {
        if (player != null) FlipSprite(Mathf.Sign(player.position.x - transform.position.x));
    }

    // ── Hệ Thống Sát Thương / Chết ──────────────────────────────────────────

    public void TakeDamage(float dmg, ulong attackerClientId = 0)
    {
        if (!NetworkUtils.HasServerAuthority || isDead) return;

        lastAttackerClientId = attackerClientId;

        if (NetworkUtils.IsOnline)
        {
            currentHealth.Value -= dmg;
            if (currentHealth.Value <= 0) Die();
        }
        else
        {
            localHealth -= dmg;
            if (localHealth <= 0) Die();
        }
    }

    public void TakeDamage(float dmg) => TakeDamage(dmg, 0);

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Cộng mạng và tiền (CoinManager đã xử lý cả Offline/Online)
        uiManager?.AddKill();
        CoinManager.Instance?.AwardCoin(coinDrop, lastAttackerClientId);

        OnDeath?.Invoke();

        if (NetworkUtils.IsOnline)
        {
            DieClientRpc();
            Invoke(nameof(DespawnEnemy), 0.5f);
        }
        else
        {
            // Offline: Chạy animation rồi biến mất
            animator?.SetBool("isDead", true);
            Destroy(gameObject, 0.5f);
        }
    }

    [ClientRpc]
    void DieClientRpc()
    {
        animator?.SetBool("isDead", true);
        // Tắt collider để không cản trở player
        GetComponent<Collider2D>().enabled = false;
    }

    void DespawnEnemy()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    // Vẽ vùng tuần tra trong Editor
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