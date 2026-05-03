using UnityEngine;
using System;

public class LavaEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 40f;
    public int coinDrop = 30;

    [Header("Lava Settings")]
    public GameObject lavaPrefab;
    public Transform shootPoint;
    public float fireRate = 2f;
    public float lavaSpeed = 5f;
    public float lavaDamage = 10f;

    private float currentHealth;
    private float fireTimer = 0f;
    private UIManager uiManager;

    public Action OnDeath;
    private bool isDead = false;
    void Start()
    {
        currentHealth = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();
    }

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            ShootLava();
            fireTimer = 0f;
        }
    }

    void ShootLava()
    {
        if (lavaPrefab == null || shootPoint == null) return;

        GameObject lava = Instantiate(lavaPrefab, shootPoint.position, Quaternion.identity);
        LavaProjectile lavaScript = lava.GetComponent<LavaProjectile>();
        if (lavaScript != null)
        {
            lavaScript.damage = lavaDamage;
            lavaScript.speed = lavaSpeed;
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (uiManager != null) uiManager.AddKill();
        DataManager.Instance?.AddCoins(coinDrop);
        OnDeath?.Invoke();

        Destroy(gameObject, 0.2f);
    }
}