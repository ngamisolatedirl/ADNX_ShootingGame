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

    // ── Network Variables ──────────────────────────────────────────────────
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> networkFacingRight = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Private State ──────────────────────────────────────────────────────
    private float localHealth;
    private float attackTimer;
    private bool movingRight = true;
    private bool isDead;
    private ulong lastAttackerClientId;

    private Transform player;
    private UIManager uiManager;
    private Animator animator;
    private Vector3 startPosition;

    public Action OnDeath;

    private enum State { Patrol, Chase, Attack, ReturnToZone }
    private State currentState;
    private State lastSyncedState;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        localHealth = maxHealth;
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!NetworkUtils.IsOnline)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            uiManager = FindFirstObjectByType<UIManager>();
            SetupCollisionIgnore();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentHealth.Value = maxHealth;

        uiManager = FindFirstObjectByType<UIManager>();
        SetupCollisionIgnore();

        if (!IsServer)
        {
            networkFacingRight.OnValueChanged += (_, newVal) => ApplyFacing(newVal);
            ApplyFacing(networkFacingRight.Value);
        }
    }

    // ── Collision Setup ────────────────────────────────────────────────────

    void SetupCollisionIgnore()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            Collider2D c = p.GetComponent<Collider2D>();
            if (c != null) Physics2D.IgnoreCollision(myCol, c);
        }
    }

    // ── Update (server / offline only) ────────────────────────────────────

    void Update()
    {
        if (!NetworkUtils.HasServerAuthority || isDead) return;

        attackTimer += Time.deltaTime;

        // Online: scan tất cả player mỗi frame → host & client được detect bình đẳng
        if (NetworkUtils.IsOnline)
        {
            UpdateNearestPlayer();
            if (player == null) return;
        }
        else if (player == null) return;

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
        SyncStateToClients();
    }

    // ── AI ─────────────────────────────────────────────────────────────────

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.1f) { movingRight = !movingRight; return; }
        MoveTowardsX(targetX, patrolSpeed);
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
        if (!isStationary && dist > attackRange * 1.2f) { currentState = State.Chase; return; }
        if (isStationary && dist > detectionRange) { currentState = State.Patrol; return; }

        FaceTarget(player.position.x);

        if (attackTimer >= attackCooldown)
        {
            animator?.SetTrigger("Attack");
            var ph = player.GetComponent<PlayerHealth>();
            if (NetworkUtils.IsOnline) ph?.TakeDamageFromServer(damage);
            else ph?.TakeDamage(damage);
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

    // ── Helpers ────────────────────────────────────────────────────────────

    void MoveTowardsX(float targetX, float speed)
    {
        float dirX = Mathf.Sign(targetX - transform.position.x);
        if (Mathf.Abs(targetX - transform.position.x) < 0.05f) return;
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);
        SetFacing(dirX > 0);
    }

    void FaceTarget(float targetX) => SetFacing(targetX > transform.position.x);

    void SetFacing(bool facingRight)
    {
        ApplyFacing(facingRight);
        if (NetworkUtils.IsOnline && networkFacingRight.Value != facingRight)
            networkFacingRight.Value = facingRight;
    }

    void ApplyFacing(bool facingRight)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;
    }

    /// <summary>Mỗi frame server scan tất cả player, chọn người gần nhất.</summary>
    void UpdateNearestPlayer()
    {
        float minDist = float.MaxValue;
        Transform nearest = null;
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == null) continue;
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist) { minDist = d; nearest = p.transform; }
        }
        if (nearest != null) player = nearest;
    }

    // ── Animation ──────────────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (animator == null) return;
        bool moving = currentState is State.Patrol or State.Chase or State.ReturnToZone;
        animator.SetBool("isWalking", moving);
        animator.SetBool("isAttacking", currentState == State.Attack);
    }

    void SyncStateToClients()
    {
        if (!NetworkUtils.IsOnline || currentState == lastSyncedState) return;
        lastSyncedState = currentState;
        bool moving = currentState is State.Patrol or State.Chase or State.ReturnToZone;
        SyncAnimationClientRpc(moving, currentState == State.Attack);
    }

    [ClientRpc]
    void SyncAnimationClientRpc(bool isWalking, bool isAttacking)
    {
        if (IsServer || animator == null) return;
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isAttacking", isAttacking);
    }

    // ── Damage / Death ─────────────────────────────────────────────────────

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
            animator?.SetBool("isDead", true);
            Destroy(gameObject, 0.5f);
        }
    }

    [ClientRpc]
    void DieClientRpc()
    {
        animator?.SetBool("isDead", true);
        GetComponent<Collider2D>().enabled = false;
    }

    void DespawnEnemy()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────

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