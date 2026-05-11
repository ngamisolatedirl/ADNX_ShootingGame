using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Fix online:
/// 1. OnNetworkSpawn: client cũng gọi FindTargetPlayer() để setup IgnoreCollision
/// 2. AttackPlayer: server dùng ServerRpc-style gọi TakeDamage trực tiếp (đã là server authority)
///    — nhưng cần target đúng player owner, dùng NetworkObject.OwnerClientId để map
/// 3. FlipSprite sync qua NetworkVariable để client thấy enemy quay đúng hướng
///    (NetworkTransform sync position, nhưng localScale.x không sync tự động)
/// 4. Animation state sync qua ClientRpc khi state thay đổi
///
/// LƯU Ý QUAN TRỌNG (làm trong Editor):
/// - Thêm component NetworkTransform vào Enemy GameObject
///   → Authority Mode: Server, bỏ tick Sync Rotation + Sync Scale
/// - Đảm bảo Enemy có NetworkObject component
/// </summary>
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

    // --- Health ---
    private float localHealth;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // FIX 3: sync hướng flip để client thấy enemy quay đúng
    // NetworkTransform không sync localScale, nên cần sync thủ công
    private NetworkVariable<bool> networkFacingRight = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
    private State lastSyncedState = State.Patrol; // để detect thay đổi state

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        animator = GetComponent<Animator>();
        startPosition = transform.position;
        localHealth = maxHealth;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!NetworkUtils.IsOnline)
            FindTargetPlayer();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // FIX 1: Tất cả (server + client) đều cần FindTargetPlayer
        // Client cần để setup IgnoreCollision với player của mình
        FindTargetPlayer();

        // FIX 3: Client lắng nghe thay đổi hướng từ server
        if (!IsServer)
        {
            networkFacingRight.OnValueChanged += OnFacingChanged;
            // Apply ngay giá trị hiện tại
            ApplyFacing(networkFacingRight.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            networkFacingRight.OnValueChanged -= OnFacingChanged;
    }

    // FIX 3: callback khi server đổi hướng
    void OnFacingChanged(bool oldVal, bool newVal) => ApplyFacing(newVal);

    void ApplyFacing(bool facingRight)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    // ── Find Player ────────────────────────────────────────────────────────

    private void FindTargetPlayer()
    {
        // Online: mỗi client/server tìm player của chính mình (owner)
        // Server cần tìm bất kỳ player nào để chase (dùng player gần nhất)
        if (NetworkUtils.IsOnline)
        {
            // Server: tìm tất cả player, sẽ chase player gần nhất trong Update
            var players = GameObject.FindGameObjectsWithTag("Player");
            float minDist = float.MaxValue;
            foreach (var p in players)
            {
                float d = Vector2.Distance(transform.position, p.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    player = p.transform;
                }
            }
        }
        else
        {
            player = GameObject.FindWithTag("Player")?.transform;
        }

        uiManager = FindFirstObjectByType<UIManager>();

        // Setup IgnoreCollision với tất cả player colliders
        if (NetworkUtils.IsOnline)
        {
            var players = GameObject.FindGameObjectsWithTag("Player");
            Collider2D myCol = GetComponent<Collider2D>();
            foreach (var p in players)
            {
                Collider2D playerCol = p.GetComponent<Collider2D>();
                if (playerCol != null && myCol != null)
                    Physics2D.IgnoreCollision(myCol, playerCol);
            }
        }
        else if (player != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            Collider2D myCol = GetComponent<Collider2D>();
            if (playerCol != null && myCol != null)
                Physics2D.IgnoreCollision(myCol, playerCol);
        }
    }

    // ── Update (chỉ server/offline chạy AI) ───────────────────────────────

    void Update()
    {
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

        // FIX 4: Sync animation state xuống client khi thay đổi
        if (NetworkUtils.IsOnline && currentState != lastSyncedState)
        {
            lastSyncedState = currentState;
            SyncAnimationClientRpc(
                currentState == State.Patrol || currentState == State.Chase || currentState == State.ReturnToZone,
                currentState == State.Attack
            );
        }
    }

    // ── AI Logic ───────────────────────────────────────────────────────────

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;

        if (Mathf.Abs(diff) < 0.1f) { movingRight = !movingRight; return; }

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * patrolSpeed * Time.deltaTime, 0, 0);
        SetFacing(dirX > 0);
    }

    void CheckDetection()
    {
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
            currentState = isStationary ? State.Attack : State.Chase;
    }

    void ChasePlayer()
    {
        // Online: chase player gần nhất (có thể đổi target)
        if (NetworkUtils.IsOnline) UpdateNearestPlayer();

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        if (dist <= attackRange) { currentState = State.Attack; return; }

        MoveTowardsX(player.position.x, moveSpeed);
    }

    void AttackPlayer()
    {
        // Online: chase player gần nhất (có thể đổi target)
        if (NetworkUtils.IsOnline) UpdateNearestPlayer();

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
            // FIX: dùng TakeDamageFromServer vì đây là server gọi trực tiếp
            // TakeDamage() yêu cầu IsOwner check nên không hoạt động khi server gọi cho client's player
            if (NetworkUtils.IsOnline)
                player.GetComponent<PlayerHealth>()?.TakeDamageFromServer(damage);
            else
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
        SetFacing(dirX > 0);
    }

    // Online: cập nhật target là player gần nhất (multiplayer)
    void UpdateNearestPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = float.MaxValue;
        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist) { minDist = d; player = p.transform; }
        }
    }

    // ── Flip / Facing ──────────────────────────────────────────────────────

    // FIX 3: server set NetworkVariable, client nhận qua OnValueChanged
    void SetFacing(bool facingRight)
    {
        ApplyFacing(facingRight);
        if (NetworkUtils.IsOnline && networkFacingRight.Value != facingRight)
            networkFacingRight.Value = facingRight;
    }

    void FlipSprite(float dirX)
    {
        if (dirX == 0) return;
        SetFacing(dirX > 0);
    }

    void FacePlayer()
    {
        if (player != null) FlipSprite(Mathf.Sign(player.position.x - transform.position.x));
    }

    // ── Animation ──────────────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (animator == null) return;
        bool isMoving = currentState == State.Patrol || currentState == State.Chase || currentState == State.ReturnToZone;
        animator.SetBool("isWalking", isMoving);
        animator.SetBool("isAttacking", currentState == State.Attack);
    }

    // FIX 4: sync animation state xuống tất cả client
    [ClientRpc]
    void SyncAnimationClientRpc(bool isWalking, bool isAttacking)
    {
        if (IsServer) return; // host tự update rồi
        if (animator == null) return;
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