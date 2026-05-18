using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// BreakableObject — Vật thể bị phá vỡ khi nhận đủ damage, sau đó drop item ngẫu nhiên.
///
/// SETUP TRONG EDITOR:
/// 1. Tạo GameObject với Sprite Renderer + Animator + Collider2D (KHÔNG Is Trigger)
/// 2. Tag GameObject là "BreakableObject"
/// 3. Gắn script này + NetworkObject (nếu online)
/// 4. Cấu hình dropTable trong Inspector
///
/// DROP TABLE:
///   Mỗi entry có: dropType, prefab (Heal/Buff), weight, minAmount/maxAmount (Coin)
///   Coin  → cộng thẳng vào ví, không cần prefab
///   Heal  → kéo HealPickup prefab vào
///   Buff  → kéo TemporaryBuffPickup prefab vào
/// </summary>
public class BreakableObject : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;

    [Header("Drop Table")]
    public DropEntry[] dropTable;

    [Header("Animation")]
    [Tooltip("Trigger 'Break' trong Animator khi vỡ")]
    public bool playBreakAnim = true;
    public float breakAnimDuration = 0.5f;

    // ── Internal ───────────────────────────────────────────────────────────

    private NetworkVariable<float> networkHP = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float localHP;
    private bool isBroken = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) networkHP.Value = maxHealth;
        isBroken = false;
    }

    void Start()
    {
        if (!NetworkUtils.IsOnline)
        {
            localHP = maxHealth;
            isBroken = false;
        }
    }

    // ── Nhận Damage ────────────────────────────────────────────────────────

    public void TakeDamage(float damage, ulong attackerClientId = 0)
    {
        if (isBroken) return;

        if (NetworkUtils.IsOnline)
        {
            if (!IsServer) return;
            networkHP.Value -= damage;
            if (networkHP.Value <= 0f)
                Break_Server(attackerClientId);
        }
        else
        {
            localHP -= damage;
            if (localHP <= 0f)
                Break_Local(attackerClientId);
        }
    }

    // ── Break — Online ─────────────────────────────────────────────────────

    void Break_Server(ulong attackerClientId)
    {
        if (isBroken) return;
        isBroken = true;

        // 1. Tắt collider ngay để không nhận damage thêm
        if (col != null) col.enabled = false;

        // 2. Drop NGAY trên server — trước khi despawn
        ExecuteDrop_Server(attackerClientId);

        // 3. Báo tất cả client play animation vỡ
        PlayBreakAnimClientRpc();

        // 4. Despawn sau khi anim xong
        StartCoroutine(DespawnAfterDelay());
    }

    /// <summary>Server xử lý drop: coin cộng ví, heal/buff spawn NetworkObject.</summary>
    void ExecuteDrop_Server(ulong attackerClientId)
    {
        DropResult drop = RollDrop();

        switch (drop.type)
        {
            case DropType.Coin:
                CoinManager.Instance?.AwardCoin(drop.coinAmount, attackerClientId);
                break;

            case DropType.Heal:
            case DropType.Buff:
                if (drop.prefab != null)
                    SpawnPickup(drop.prefab);
                break;
        }
    }

    [ClientRpc]
    void PlayBreakAnimClientRpc()
    {
        StartCoroutine(PlayBreakThenHide());
    }

    IEnumerator PlayBreakThenHide()
    {
        if (playBreakAnim && animator != null)
        {
            animator.SetTrigger("Break");
            yield return new WaitForSeconds(breakAnimDuration);
        }
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;
    }

    IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(breakAnimDuration + 0.3f);
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    // ── Break — Offline ────────────────────────────────────────────────────

    void Break_Local(ulong attackerClientId)
    {
        if (isBroken) return;
        isBroken = true;

        if (col != null) col.enabled = false;

        // Drop ngay, không chờ anim
        ExecuteDrop_Local(attackerClientId);

        StartCoroutine(PlayBreakThenDestroy());
    }

    void ExecuteDrop_Local(ulong attackerClientId)
    {
        DropResult drop = RollDrop();

        switch (drop.type)
        {
            case DropType.Coin:
                CoinManager.Instance?.AwardCoin(drop.coinAmount, 0);
                break;

            case DropType.Heal:
            case DropType.Buff:
                if (drop.prefab != null)
                    Instantiate(drop.prefab, transform.position, Quaternion.identity);
                break;
        }
    }

    IEnumerator PlayBreakThenDestroy()
    {
        if (playBreakAnim && animator != null)
        {
            animator.SetTrigger("Break");
            yield return new WaitForSeconds(breakAnimDuration);
        }
        Destroy(gameObject);
    }

    // ── Drop Roll ──────────────────────────────────────────────────────────

    DropResult RollDrop()
    {
        if (dropTable == null || dropTable.Length == 0)
            return new DropResult { type = DropType.Nothing };

        float totalWeight = 0f;
        foreach (var entry in dropTable) totalWeight += entry.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < dropTable.Length; i++)
        {
            cumulative += dropTable[i].weight;
            if (roll <= cumulative)
            {
                var entry = dropTable[i];
                return new DropResult
                {
                    type = entry.dropType,
                    coinAmount = Random.Range(entry.minAmount, entry.maxAmount + 1),
                    prefab = entry.prefab
                };
            }
        }

        return new DropResult { type = DropType.Nothing };
    }

    // ── Spawn Pickup (Online) ──────────────────────────────────────────────

    void SpawnPickup(GameObject prefab)
    {
        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        if (no != null)
            no.Spawn();
        else
            Debug.LogWarning($"[BreakableObject] Prefab '{prefab.name}' thiếu NetworkObject component!");
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) Gizmos.DrawWireCube(transform.position, c.bounds.size);
    }
}

// ── Data Types ─────────────────────────────────────────────────────────────

public enum DropType { Nothing, Coin, Heal, Buff }

[System.Serializable]
public class DropEntry
{
    [Tooltip("Loại drop")]
    public DropType dropType;

    [Tooltip("Prefab spawn ra (để trống nếu là Coin)")]
    public GameObject prefab;

    [Tooltip("Xác suất tương đối. Ví dụ Coin=60, Heal=30, Buff=10")]
    public float weight = 50f;

    [Tooltip("Số coin tối thiểu (chỉ dùng cho Coin)")]
    public int minAmount = 5;

    [Tooltip("Số coin tối đa (chỉ dùng cho Coin)")]
    public int maxAmount = 15;
}

struct DropResult
{
    public DropType type;
    public int coinAmount;
    public GameObject prefab;
}