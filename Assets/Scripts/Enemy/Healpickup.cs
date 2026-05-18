using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// HealPickup — Object hồi máu khi player chạm vào.
///
/// SETUP TRONG EDITOR:
/// 1. Tạo GameObject, gắn Sprite Renderer + Animator (animation tự làm)
/// 2. Thêm Collider2D → tick "Is Trigger"
/// 3. Gắn script này vào
/// 4. Nếu Online: thêm NetworkObject component
///
/// CÁC TUỲ CHỌN (Inspector):
///   healAmount        — lượng máu hồi
///   disappearAfterUse — true: biến mất sau khi dùng | false: vẫn còn nhưng không trigger nữa
///   respawnTime       — > 0: tự hiện lại sau N giây (chỉ khi disappearAfterUse = true)
///   playAnimOnPickup  — phát trigger "Pickup" trong Animator trước khi ẩn
/// </summary>
public class HealPickup : NetworkBehaviour
{
    [Header("Heal Settings")]
    [Tooltip("Lượng máu hồi cho player")]
    public float healAmount = 30f;

    [Header("Behaviour")]
    [Tooltip("Biến mất sau khi dùng?")]
    public bool disappearAfterUse = true;

    [Tooltip("Thời gian (giây) để tự hiện lại. 0 = không hồi sinh. Chỉ dùng khi disappearAfterUse = true")]
    public float respawnTime = 0f;

    [Tooltip("Phát trigger 'Pickup' trong Animator trước khi ẩn")]
    public bool playAnimOnPickup = true;

    // ── Internal ───────────────────────────────────────────────────────────

    private NetworkVariable<bool> isUsed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool localUsed = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        isUsed.OnValueChanged += OnUsedChanged;
        ApplyUsedState(isUsed.Value);
    }

    public override void OnNetworkDespawn()
    {
        isUsed.OnValueChanged -= OnUsedChanged;
    }

    // ── Trigger ────────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (NetworkUtils.IsOnline)
        {
            if (!IsServer) return;
            if (isUsed.Value) return;

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.HealFromServer(healAmount);

            MarkUsed_Server();
        }
        else
        {
            if (localUsed) return;
            localUsed = true;

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(healAmount);

            HandleUsed_Offline();
        }
    }

    // ── Server Logic ───────────────────────────────────────────────────────

    void MarkUsed_Server()
    {
        isUsed.Value = true;

        if (disappearAfterUse && respawnTime > 0f)
            StartCoroutine(RespawnRoutine_Server());
    }

    IEnumerator RespawnRoutine_Server()
    {
        yield return new WaitForSeconds(respawnTime);
        isUsed.Value = false;
    }

    // ── Sync Callback ──────────────────────────────────────────────────────

    void OnUsedChanged(bool oldVal, bool newVal) => ApplyUsedState(newVal);

    void ApplyUsedState(bool used)
    {
        if (used)
        {
            if (disappearAfterUse)
                StartCoroutine(PlayPickupThenSetVisible(false));
            else
                if (col != null) col.enabled = false; // giữ visual, tắt collider
        }
        else
        {
            SetVisible(true); // hồi sinh
        }
    }

    IEnumerator PlayPickupThenSetVisible(bool visible)
    {
        if (playAnimOnPickup && animator != null)
        {
            animator.SetTrigger("Pickup");
            yield return null;
            float length = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
        }
        SetVisible(visible);
    }

    void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (col != null) col.enabled = visible;
    }

    // ── Offline Logic ──────────────────────────────────────────────────────

    void HandleUsed_Offline()
    {
        if (disappearAfterUse)
        {
            if (respawnTime > 0f)
                StartCoroutine(RespawnRoutine_Offline());
            else
                StartCoroutine(PlayPickupThenDestroy());
        }
        else
        {
            if (col != null) col.enabled = false;
        }
    }

    IEnumerator PlayPickupThenDestroy()
    {
        if (playAnimOnPickup && animator != null)
        {
            animator.SetTrigger("Pickup");
            yield return null;
            float length = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
        }
        Destroy(gameObject);
    }

    IEnumerator RespawnRoutine_Offline()
    {
        SetVisible(false);
        yield return new WaitForSeconds(respawnTime);
        localUsed = false;
        SetVisible(true);
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Collider2D c = GetComponent<Collider2D>();
        if (c is CircleCollider2D cc)
            Gizmos.DrawWireSphere(transform.position, cc.radius);
        else if (c != null)
            Gizmos.DrawWireCube(transform.position, c.bounds.size);
    }
}