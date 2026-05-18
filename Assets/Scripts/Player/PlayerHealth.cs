using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

/// <summary>
/// Fix:
/// 1. Thanh máu tụt hết: force fire OnHealthChanged(max→0) trong DieClientRpc
/// 2. Camera không chuyển: DieClientRpc gọi PlayerCameraController.OnPlayerDied() trên owner
/// 3. Restart bị đơ: OnNetworkSpawn reset isDead, restore visuals, re-enable components
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;
    public float deathHideDelay = 1.5f;

    public event Action OnDied;
    public event Action<float, float, float> OnHealthChanged;

    private NetworkVariable<float> networkHP = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float localHP;
    private bool isDead = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            networkHP.Value = maxHealth;

        networkHP.OnValueChanged += OnHPChanged;

        // FIX 3: reset toàn bộ state mỗi lần spawn (kể cả sau restart)
        isDead = false;
        RestoreVisuals();

        var move = GetComponent<MovePlayer>();
        var shoot = GetComponent<Shooting>();
        if (move != null) move.enabled = true;
        if (shoot != null) shoot.enabled = true;

        OnHealthChanged?.Invoke(networkHP.Value, networkHP.Value, maxHealth);
    }

    void Start()
    {
        if (!NetworkUtils.IsOnline)
        {
            localHP = maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(maxHealth, maxHealth, maxHealth);
        }
    }

    public override void OnNetworkDespawn()
    {
        networkHP.OnValueChanged -= OnHPChanged;
    }

    void OnHPChanged(float oldVal, float newVal)
    {
        OnHealthChanged?.Invoke(oldVal, newVal, maxHealth);
    }

    // ── Damage ─────────────────────────────────────────────────────────────

    /// <summary>Gọi từ owner (DeathZone, hazard, v.v.)</summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (NetworkUtils.IsOnline)
        {
            if (IsOwner)
                TakeDamageServerRpc(damage);
        }
        else
        {
            ApplyDamageLocal(damage);
        }
    }

    /// <summary>Gọi từ Server trực tiếp (MeleeEnemy.AttackPlayer).</summary>
    public void TakeDamageFromServer(float damage)
    {
        if (!IsServer) return;
        ApplyDamageServer(damage);
    }

    [ServerRpc]
    void TakeDamageServerRpc(float damage) => ApplyDamageServer(damage);

    void ApplyDamageServer(float damage)
    {
        if (isDead) return;
        networkHP.Value = Mathf.Clamp(networkHP.Value - damage, 0, maxHealth);
        if (networkHP.Value <= 0)
            DieServer();
    }

    void ApplyDamageLocal(float damage)
    {
        float oldHP = localHP;
        localHP = Mathf.Clamp(localHP - damage, 0, maxHealth);
        OnHealthChanged?.Invoke(oldHP, localHP, maxHealth);
        if (localHP <= 0)
            Die();
    }

    // ── Death ──────────────────────────────────────────────────────────────

    public void Die()
    {
        if (isDead) return;

        if (NetworkUtils.IsOnline)
        {
            if (IsOwner) DieServerRpc();
        }
        else
        {
            DieLocal();
        }
    }

    [ServerRpc]
    public void FallDeathServerRpc() => DieServer();

    [ServerRpc]
    void DieServerRpc() => DieServer();

    void DieServer()
    {
        if (isDead) return;
        isDead = true;
        networkHP.Value = 0;

        GameManager.Instance?.ReportPlayerDeath(OwnerClientId);
        DieClientRpc();

        StartCoroutine(DespawnAfterDelay());
    }

    IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(deathHideDelay);
        if (IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }

    void DieLocal()
    {
        if (isDead) return;
        isDead = true;
        localHP = 0;

        OnHealthChanged?.Invoke(maxHealth, 0, maxHealth);
        OnDied?.Invoke();
        GetComponent<PlayerAnimator>()?.PlayDeath();

        var move = GetComponent<MovePlayer>();
        var shoot = GetComponent<Shooting>();
        if (move != null) move.enabled = false;
        if (shoot != null) shoot.enabled = false;

        StartCoroutine(HideAfterDelay());
        GameManager.Instance?.GameOver();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        isDead = true;

        // FIX 1: force UI tụt về 0 ngay — NetworkVariable sync có thể đến sau ClientRpc
        OnHealthChanged?.Invoke(maxHealth, 0, maxHealth);

        OnDied?.Invoke();
        GetComponent<PlayerAnimator>()?.PlayDeath();

        if (IsOwner)
        {
            var move = GetComponent<MovePlayer>();
            var shoot = GetComponent<Shooting>();
            if (move != null) move.enabled = false;
            if (shoot != null) shoot.enabled = false;

            // FIX 2: chuyển camera sang spectate player còn sống
            GetComponent<PlayerCameraController>()?.OnPlayerDied();
        }

        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(deathHideDelay);
        HideVisuals();
    }

    // ── Visuals ────────────────────────────────────────────────────────────

    void HideVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;
    }

    void RestoreVisuals()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = true;
    }

    // ── Reset ──────────────────────────────────────────────────────────────

    public void ResetHealth()
    {
        isDead = false;
        RestoreVisuals();

        if (NetworkUtils.IsOnline)
        {
            if (IsServer)
                networkHP.Value = maxHealth;
            OnHealthChanged?.Invoke(0, maxHealth, maxHealth);
        }
        else
        {
            float old = localHP;
            localHP = maxHealth;
            OnHealthChanged?.Invoke(old, maxHealth, maxHealth);
        }
    }

    // ── Getters ────────────────────────────────────────────────────────────

    public float GetHealth() =>
        NetworkUtils.IsOnline ? networkHP.Value : localHP;

    public bool IsDead => isDead;

    // ════════════════════════════════════════════════════════════════════════
    // PATCH — Thêm vào class PlayerHealth (sau phần // ── Damage ──)
    // ════════════════════════════════════════════════════════════════════════

    // ── Heal (offline) ──────────────────────────────────────────────────────

    /// <summary>Hồi máu offline, gọi trực tiếp trên client.</summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        float oldHP = localHP;
        localHP = Mathf.Clamp(localHP + amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(oldHP, localHP, maxHealth);
    }

    // ── Heal (online, server gọi trực tiếp) ────────────────────────────────

    /// <summary>
    /// Server gọi để hồi máu cho player bất kỳ (ví dụ: HealPickup).
    /// Tương tự TakeDamageFromServer nhưng cộng máu.
    /// </summary>
    public void HealFromServer(float amount)
    {
        if (!IsServer || isDead) return;

        float oldHP = networkHP.Value;
        networkHP.Value = Mathf.Clamp(networkHP.Value + amount, 0f, maxHealth);
        // OnHPChanged sẽ tự fire vì networkHP thay đổi → UI tự cập nhật
    }
}