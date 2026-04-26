using UnityEngine;
using System;

public class ShootingEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 2f;

    [Header("Patrol Zone")]
    public Transform zoneStart;
    public Transform zoneEnd;
    public float patrolSpeed = 1.5f;

    [Header("Detection & Shooting")]
    public float detectionRange = 8f;
    public float shootRange = 6f;       // Dừng lại và bắn trong tầm này
    public float fireRate = 1.5f;       // Thời gian giữa 2 phát bắn
    public float bulletSpeed = 8f;
    public float bulletDamage = 10f;
    public GameObject bulletPrefab;
    public Transform shootPoint;

    private float currentHealth;
    private float fireTimer = 0f;
    private bool movingRight = true;
    private Transform player;
    private UIManager uiManager;

    public Action OnDeath;

    private enum State { Patrol, Chase, Shoot, ReturnToZone }
    private State currentState = State.Patrol;

    [Header("Behaviour")]
    public bool isStationary = false;
    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindWithTag("Player").transform;
        uiManager = FindFirstObjectByType<UIManager>();

        // Lock rotation
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;

        if (isStationary)
        {
            // Chỉ check detection khi chưa thấy player
            if (currentState != State.Shoot)
                CheckDetection();
            else
                ShootPlayer();
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
            case State.Shoot:
                ShootPlayer();
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
        float diff = targetX - transform.position.x;

        // Đi hết zone mới đổi hướng
        if (Mathf.Abs(diff) < 0.05f)
        {
            movingRight = !movingRight;
            return;
        }

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * patrolSpeed * Time.deltaTime, 0, 0);

        // Chỉ flip sprite, KHÔNG xoay transform
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    void MoveTowardsX(float targetX, float speed)
    {
        float diff = targetX - transform.position.x;
        if (Mathf.Abs(diff) < 0.05f) return;

        float dirX = Mathf.Sign(diff);
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);

        // Chỉ flip sprite theo trục X, không đụng Y và Z
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    void CheckDetection()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);

        // Chỉ set state khi đang Patrol thôi
        if (currentState != State.Patrol) return;

        if (dist <= detectionRange)
            currentState = isStationary ? State.Shoot : State.Chase;
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

        // Đủ gần → dừng bắn
        if (dist <= shootRange)
        {
            currentState = State.Shoot;
            return;
        }

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

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
            bulletScript.SetDirection(direction);
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

    //void MoveTowardsX(float targetX, float speed)
    //{
    //    float diff = targetX - transform.position.x;
    //    if (Mathf.Abs(diff) < 0.05f) return;

    //    float dirX = Mathf.Sign(diff);
    //    transform.position += new Vector3(dirX * speed * Time.deltaTime, 0, 0);

    //    Vector3 scale = transform.localScale;
    //    scale.x = Mathf.Abs(scale.x) * dirX;
    //    transform.localScale = scale;
    //}

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
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}