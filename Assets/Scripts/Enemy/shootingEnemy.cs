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

    // Health
    private float localHealth;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float fireTimer = 0f;
    private bool movingRight = true;
    private Transform player;
    private UIManager uiManager;
    private bool isDead = false;
    private ulong lastAttackerClientId = 0;

    public Action OnDeath;

    private enum State { Patrol, Chase, Shoot, ReturnToZone }
    private State currentState = State.Patrol;

    // ── Lifecycle ──────────────────────────────────────────────────

    void Start()
    {
        localHealth = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();

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
            FindTargetPlayer();
        }
    }

    void FindTargetPlayer()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        uiManager = FindFirstObjectByType<UIManager>();
    }

    // ── Update / AI ────────────────────────────────────────────────

    void Update()
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isDead) return;

        if (player == null) { FindTargetPlayer(); return; }

        fireTimer += Time.deltaTime;

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
            currentState = isStationary ? State.Shoot : State.Chase;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        if (dist <= shootRange) { currentState = State.Shoot; return; }

        MoveTowardsX(player.position.x, moveSpeed);
    }

    void ShootPlayer()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);

        if (!isStationary)
        {
            if (dist > shootRange * 1.2f) { currentState = State.Chase; return; }
            if (dist > detectionRange) { currentState = State.ReturnToZone; return; }
        }

        FacePlayer();

        if (fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || shootPoint == null) return;

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        Vector2 direction = new Vector2(dirX, 0);

        if (NetworkUtils.IsOnline)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();

            if (bulletScript != null)
            {
                bulletScript.damage = bulletDamage;
                bulletScript.speed = bulletSpeed;
            }

            NetworkObject netObj = bullet.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Set direction TRƯỚC khi Spawn
                // giống pattern trong Shooting.cs của player
                bulletScript?.SetDirection(direction);
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
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.damage = bulletDamage;
                bulletScript.speed = bulletSpeed;
                bulletScript.SetDirection(direction);
            }
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

    void MoveTowardsX(float targetX, float speed)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) return;

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);
        FlipSprite(dirX);
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
        if (player != null)
            FlipSprite(Mathf.Sign(player.position.x - transform.position.x));
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
    void DieClientRpc()
    {
        GetComponent<Collider2D>().enabled = false;
    }

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