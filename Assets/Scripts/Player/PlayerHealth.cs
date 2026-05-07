using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Quản lý máu player.
/// - Online: HP sync qua NetworkVariable, damage xử lý trên server.
/// - Offline: chạy như MonoBehaviour thường.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth = 100f;

    // NetworkVariable để sync HP cho tất cả client (hiển thị UI)
    private NetworkVariable<float> networkHP = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Offline: dùng local field
    private float localHP;

    private bool isDead = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            networkHP.Value = maxHealth;

        networkHP.OnValueChanged += OnHPChanged;
    }

    void Start()
    {
        if (!NetworkUtils.IsOnline)
            localHP = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        networkHP.OnValueChanged -= OnHPChanged;
    }

    void OnHPChanged(float oldVal, float newVal)
    {
        // UI tự cập nhật qua GetHealth()
    }

    // ── Collision Damage ───────────────────────────────────────────────────

    void OnCollisionStay2D(Collision2D collision)
    {
        // Chỉ owner (hoặc offline) nhận damage từ collision
        if (NetworkUtils.IsOnline && !IsOwner) return;

        if (collision.gameObject.CompareTag("Enemy") ||
            collision.gameObject.CompareTag("Enemy2") ||
            collision.gameObject.CompareTag("MeleeEnemy"))
        {
            TakeDamage(5f * Time.deltaTime);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (NetworkUtils.IsOnline)
        {
            // Client gửi lên server xử lý
            if (IsOwner)
                TakeDamageServerRpc(damage);
        }
        else
        {
            // Offline: xử lý local
            ApplyDamage(damage);
        }
    }

    [ServerRpc]
    void TakeDamageServerRpc(float damage)
    {
        ApplyDamageServer(damage);
    }

    // Server apply damage
    void ApplyDamageServer(float damage)
    {
        if (isDead) return;
        networkHP.Value = Mathf.Clamp(networkHP.Value - damage, 0, maxHealth);
        if (networkHP.Value <= 0)
            DieServer();
    }

    // Offline apply damage
    void ApplyDamage(float damage)
    {
        localHP = Mathf.Clamp(localHP - damage, 0, maxHealth);
        if (localHP <= 0)
            Die();
    }

    // ── Death ──────────────────────────────────────────────────────────────

    /// <summary>Gọi từ DeathZone hoặc bất kỳ external source nào.</summary>
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
    void DieServerRpc() => DieServer();

    void DieServer()
    {
        if (isDead) return;
        isDead = true;
        networkHP.Value = 0;

        ulong clientId = IsHost
            ? NetworkManager.Singleton.LocalClientId
            : OwnerClientId;

        // Báo GameManager
        GameManager.Instance?.ReportPlayerDeath(clientId);

        // Thông báo client play animation chết
        DieClientRpc();
    }

    void DieLocal()
    {
        if (isDead) return;
        isDead = true;
        localHP = 0;

        GetComponent<PlayerAnimator>()?.PlayDeath();
        GameManager.Instance?.GameOver();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        isDead = true;
        GetComponent<PlayerAnimator>()?.PlayDeath();

        // Disable input chỉ trên owner
        if (IsOwner)
        {
            GetComponent<MovePlayer>().enabled = false;
            GetComponent<Shooting>().enabled = false;
        }
    }

    // ── Reset ──────────────────────────────────────────────────────────────

    public void ResetHealth()
    {
        isDead = false;
        if (NetworkUtils.IsOnline)
        {
            if (IsServer) networkHP.Value = maxHealth;
        }
        else
        {
            localHP = maxHealth;
        }
    }

    // ── Getters ────────────────────────────────────────────────────────────

    public float GetHealth() =>
        NetworkUtils.IsOnline ? networkHP.Value : localHP;

    public bool IsDead => isDead;
}
