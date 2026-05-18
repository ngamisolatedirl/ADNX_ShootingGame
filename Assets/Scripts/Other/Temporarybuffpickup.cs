using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// TemporaryBuffPickup — Pickup buff tạm thời khi player chạm vào.
/// Dùng chung cơ chế với HealPickup (disappear + respawn).
///
/// SETUP:
/// 1. Tạo prefab với Sprite Renderer + Collider2D (Is Trigger) + script này
/// 2. Nếu Online: thêm NetworkObject
/// 3. Chọn buffType và duration trong Inspector
///
/// BUFF TYPES:
///   SpeedBoost   — nhân moveSpeed của MovePlayer
///   DamageBoost  — nhân damage của Shooting / MeleeAttack
///   Invincible   — tạm thời không nhận damage (set isDead-proof flag trong PlayerHealth)
/// </summary>
public class TemporaryBuffPickup : NetworkBehaviour
{
    [Header("Buff Settings")]
    public BuffType buffType = BuffType.SpeedBoost;

    [Tooltip("Hệ số nhân (x1.5 = tăng 50%). Dùng cho Speed và Damage.")]
    public float multiplier = 1.5f;

    [Tooltip("Thời gian buff (giây)")]
    public float duration = 5f;

    [Header("Behaviour")]
    public bool disappearAfterUse = true;
    public float respawnTime = 0f;
    public bool playAnimOnPickup = true;

    // ── Internal ───────────────────────────────────────────────────────────

    private NetworkVariable<bool> isUsed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            // Server xác nhận, sau đó báo đúng client áp dụng buff
            if (!IsServer) return;
            if (isUsed.Value) return;

            var nb = other.GetComponent<NetworkBehaviour>();
            if (nb == null) return;

            ApplyBuffClientRpc(nb.OwnerClientId);
            MarkUsed_Server();
        }
        else
        {
            if (localUsed) return;
            localUsed = true;

            ApplyBuffToPlayer(other.gameObject);
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

    // ── ClientRpc ──────────────────────────────────────────────────────────

    /// <summary>Server chỉ đúng client sở hữu player áp dụng buff.</summary>
    [ClientRpc]
    void ApplyBuffClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        // Tìm player của client này
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var nb = p.GetComponent<NetworkBehaviour>();
            if (nb != null && nb.IsOwner)
            {
                ApplyBuffToPlayer(p);
                break;
            }
        }
    }

    // ── Apply Buff ─────────────────────────────────────────────────────────

    void ApplyBuffToPlayer(GameObject playerObj)
    {
        switch (buffType)
        {
            case BuffType.SpeedBoost:
                var move = playerObj.GetComponent<MovePlayer>();
                if (move != null)
                    StartCoroutine(SpeedBuffRoutine(move));
                break;

            case BuffType.DamageBoost:
                var shoot = playerObj.GetComponent<Shooting>();
                if (shoot != null)
                    StartCoroutine(DamageBuffRoutine(shoot));
                break;

            //case BuffType.Invincible:
            //    var ph = playerObj.GetComponent<PlayerHealth>();
            //    if (ph != null)
            //        StartCoroutine(InvincibleRoutine(ph));
            //    break;
        }
    }

    IEnumerator SpeedBuffRoutine(MovePlayer move)
    {
        move.moveSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        move.moveSpeed /= multiplier;
    }

    IEnumerator DamageBuffRoutine(Shooting shoot)
    {
        shoot.damage *= multiplier;
        yield return new WaitForSeconds(duration);
        shoot.damage /= multiplier;
    }

    //IEnumerator InvincibleRoutine(PlayerHealth ph)
    //{
    //    ph.SetInvincible(true);
    //    yield return new WaitForSeconds(duration);
    //    ph.SetInvincible(false);
    //}

    // ── Sync State ─────────────────────────────────────────────────────────

    void OnUsedChanged(bool oldVal, bool newVal) => ApplyUsedState(newVal);

    void ApplyUsedState(bool used)
    {
        if (used)
        {
            if (disappearAfterUse)
                StartCoroutine(PlayPickupThenHide());
            else
                if (col != null) col.enabled = false;
        }
        else
        {
            SetVisible(true);
        }
    }

    IEnumerator PlayPickupThenHide()
    {
        if (playAnimOnPickup && animator != null)
        {
            animator.SetTrigger("Pickup");
            yield return null;
            float length = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
        }
        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (col != null) col.enabled = visible;
    }

    // ── Offline ────────────────────────────────────────────────────────────

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
}

// ── Enum ───────────────────────────────────────────────────────────────────

public enum BuffType
{
    SpeedBoost,
    DamageBoost,
    Invincible
}