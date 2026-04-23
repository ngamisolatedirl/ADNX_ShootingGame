using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    [Header("Boss Stats")]
    public float maxHealth = 200f;
    private float currentHealth;

    [Header("Shooting")]
    public GameObject bossBulletPrefab;
    public Transform shootingPoint;
    public float shootInterval = 2f;

    [Header("Win Zone")]
    public GameObject winZonePrefab;
    public Transform winZoneSpawnPoint;

    private float shootTimer = 0f;
    private UIManager uiManager;

    void Start()
    {
        currentHealth = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();
    }

    void Update()
    {
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            Shoot();
            shootTimer = 0f;
        }
    }

    void Shoot()
    {
        if (bossBulletPrefab == null || shootingPoint == null) return;
        Instantiate(bossBulletPrefab, shootingPoint.position, Quaternion.identity);
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
        // Spawn WinZone
        if (winZonePrefab != null && winZoneSpawnPoint != null)
            Instantiate(winZonePrefab, winZoneSpawnPoint.position, Quaternion.identity);

        Destroy(gameObject);
    }
}