using UnityEngine;
using Unity.Netcode;
using System;

public class LavaEnemy : NetworkBehaviour
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

    private float localHealth;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float fireTimer = 0f;
    private UIManager uiManager;
    private bool isDead = false;
    private ulong lastAttackerClientId = 0;

    public Action OnDeath;

    // ── Lifecycle ──────────────────────────────────────────────────

    void Start()
    {
        localHealth = maxHealth;
        uiManager = FindFirstObjectByType<UIManager>();

        if (!NetworkUtils.IsOnline)
            localHealth = maxHealth;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    // ── Update ─────────────────────────────────────────────────────

    void Update()
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isDead) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            ShootLava();
            fireTimer = 0f;
        }
    }

    // ── Shoot ──────────────────────────────────────────────────────

    void ShootLava()
    {
        if (lavaPrefab == null || shootPoint == null) return;

        if (NetworkUtils.IsOnline)
        {
            GameObject lava = Instantiate(lavaPrefab, shootPoint.position, Quaternion.identity);
            LavaProjectile lavaScript = lava.GetComponent<LavaProjectile>();

            if (lavaScript != null)
            {
                lavaScript.damage = lavaDamage;
                lavaScript.speed = lavaSpeed;
            }

            NetworkObject netObj = lava.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            else
            {
                Debug.LogError("[LavaEnemy] LavaProjectile prefab thiếu NetworkObject!");
                Destroy(lava);
            }
        }
        else
        {
            GameObject lava = Instantiate(lavaPrefab, shootPoint.position, Quaternion.identity);
            LavaProjectile lavaScript = lava.GetComponent<LavaProjectile>();
            if (lavaScript != null)
            {
                lavaScript.damage = lavaDamage;
                lavaScript.speed = lavaSpeed;
            }
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
            Destroy(gameObject, 0.2f);
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
}