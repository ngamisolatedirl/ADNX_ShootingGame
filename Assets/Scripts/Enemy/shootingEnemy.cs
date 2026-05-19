using System;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class ShootingEnemy : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 2f;
    public int coinDrop = 30;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 1.5f;

    [Header("Detection & Shooting")]
    public float detectionRange = 8f;
    public float shootRange = 6f;
    public float fireRate = 1.5f;
    public float bulletSpeed = 8f;
    public float bulletDamage = 10f;
    public GameObject bulletPrefab;
    public Transform shootPoint;

    [Header("Behaviour")]
    public bool isStationary = false;

    // ── Network Variables ──────────────────────────────────────────
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Private State ──────────────────────────────────────────────
    private float localHealth;
    private float fireTimer;
    private bool movingRight = true;
    private bool isDead;
    private ulong lastAttackerClientId;

    private Transform player;
    private UIManager uiManager;

    public Action OnDeath;

    private enum State { Patrol, Chase, Shoot, ReturnToZone }
    private State currentState;

    // ── Lifecycle ──────────────────────────────────────────────────

    void Start()
    {
        localHealth = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;

        if (!NetworkUtils.IsOnline)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            SetupCollisionIgnore();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentHealth.Value = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();
        SetupCollisionIgnore();
    }

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

    // ── Update / AI ────────────────────────────────────────────────

    void Update()
    {
        if (!NetworkUtils.HasServerAuthority || isDead) return;

        fireTimer += Time.deltaTime;

        if (NetworkUtils.IsOnline)
        {
            UpdateNearestPlayer();
            if (player == null) return;
        }
        else if (player == null) return;

        if (isStationary)
        {
            if (currentState != State.Shoot) CheckDetection();
            else ShootPlayer();
            return;
        }

        switch (currentState)
        {
            case State.Patrol: Patrol(); CheckDetection(); break;
            case State.Chase: ChasePlayer(); break;
            case State.Shoot: ShootPlayer(); break;
            case State.ReturnToZone: ReturnToZone(); break;
        }
    }

    void Patrol()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float targetX = movingRight ? zoneEnd.position.x : zoneStart.position.x;
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) { movingRight = !movingRight; return; }
        MoveTowardsX(targetX, patrolSpeed);
    }

    void CheckDetection()
    {
        if (Vector2.Distance(transform.position, player.position) <= detectionRange)
            currentState = isStationary ? State.Shoot : State.Chase;
    }

    void ChasePlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        if (dist <= shootRange) { currentState = State.Shoot; return; }
        MoveTowardsX(player.position.x, moveSpeed);
    }

    void ShootPlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (!isStationary)
        {
            if (dist > shootRange * 1.2f) { currentState = State.Chase; return; }
            if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        }

        FaceTarget(player.position.x);

        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void ReturnToZone()
    {
        if (zoneStart == null || zoneEnd == null) return;
        float centerX = (zoneStart.position.x + zoneEnd.position.x) / 2f;
        MoveTowardsX(centerX, moveSpeed);
        bool inZone = transform.position.x >= zoneStart.position.x
                   && transform.position.x <= zoneEnd.position.x;
        if (inZone) currentState = State.Patrol;
    }

    // ── Helpers ────────────────────────────────────────────────────

    void MoveTowardsX(float targetX, float speed)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) return;
        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);
        SetFacing(dirX > 0);
    }

    void FaceTarget(float targetX) => SetFacing(targetX > transform.position.x);

    void SetFacing(bool facingRight)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;
    }

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

    // ── Shoot ──────────────────────────────────────────────────────

    void Shoot()
    {
        if (bulletPrefab == null || shootPoint == null) return;

        Vector2 direction = new Vector2(
            Mathf.Sign(player.position.x - transform.position.x), 0);

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        EnemyBullet bs = bullet.GetComponent<EnemyBullet>();
        if (bs != null)
        {
            bs.damage = bulletDamage;
            bs.speed = bulletSpeed;
        }

        if (NetworkUtils.IsOnline)
        {
            NetworkObject netObj = bullet.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                bs?.SetDirection(direction);
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("[ShootingEnemy] EnemyBullet prefab thiếu NetworkObject!");
                Destroy(bullet);
            }
        }
        else
        {
            bs?.SetDirection(direction);
        }
    }

    // ── Damage / Death ─────────────────────────────────────────────

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
            Destroy(gameObject, 0.5f);
        }
    }

    [ClientRpc]
    void DieClientRpc() => GetComponent<Collider2D>().enabled = false;

    void DespawnEnemy()
    {
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    // ── Gizmos ─────────────────────────────────────────────────────

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
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}