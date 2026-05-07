using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Camera follow player.
/// - Khi player chết → tìm player còn sống gần nhất để follow.
/// - Offline: follow player duy nhất.
/// - Online: mỗi client follow player của mình; khi chết chuyển sang người khác.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public Vector2 offset = Vector2.zero;

    // Target hiện tại
    private Transform currentTarget;

    // Cache tất cả PlayerHealth trong scene
    private List<PlayerHealth> allPlayers = new List<PlayerHealth>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        RefreshPlayerList();
        FindOwnerTarget();
    }

    void LateUpdate()
    {
        if (currentTarget == null)
        {
            // Target đã bị destroy, tìm lại
            FindNearestAlivePlayer(transform.position);
            if (currentTarget == null) return;
        }

        Vector3 targetPos = new Vector3(
            currentTarget.position.x + offset.x,
            transform.position.y + offset.y,   // chỉ follow trục X (hoặc đổi nếu muốn Y)
            transform.position.z
        );

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    // ── Setup ──────────────────────────────────────────────────────────────

    void RefreshPlayerList()
    {
        allPlayers.Clear();
        var all = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        allPlayers.AddRange(all);
    }

    void FindOwnerTarget()
    {
        if (!NetworkUtils.IsOnline)
        {
            // Offline: follow player đầu tiên trong scene
            if (allPlayers.Count > 0)
                currentTarget = allPlayers[0].transform;
            return;
        }

        // Online: tìm player thuộc về local client
        foreach (var ph in allPlayers)
        {
            var nb = ph.GetComponent<NetworkBehaviour>();
            if (nb != null && nb.IsOwner)
            {
                currentTarget = ph.transform;
                return;
            }
        }
    }

    // ── Khi Player Chết ────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ GameManager (ClientRpc) khi 1 player chết.
    /// </summary>
    public void OnPlayerDied(ulong deadClientId)
    {
        if (!NetworkUtils.IsOnline) return;

        // Kiểm tra xem target hiện tại có phải là player vừa chết không
        if (currentTarget == null) { FindNearestAlivePlayer(transform.position); return; }

        var nb = currentTarget.GetComponent<NetworkBehaviour>();
        if (nb != null && nb.OwnerClientId == deadClientId)
        {
            // Camera đang follow người vừa chết → chuyển sang người khác
            FindNearestAlivePlayer(currentTarget.position);
        }
    }

    void FindNearestAlivePlayer(Vector3 fromPos)
    {
        // Refresh list (player mới có thể spawn sau)
        RefreshPlayerList();

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var ph in allPlayers)
        {
            if (ph == null || ph.IsDead) continue;

            // Online: ưu tiên không follow chính mình nếu mình chết
            float dist = Vector3.Distance(fromPos, ph.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = ph.transform;
            }
        }

        if (nearest != null)
        {
            currentTarget = nearest;
            Debug.Log($"[Camera] Chuyển follow sang: {nearest.name}");
        }
    }

    // ── Public ─────────────────────────────────────────────────────────────

    public void SetTarget(Transform t) => currentTarget = t;

    public void RegisterPlayer(PlayerHealth ph)
    {
        if (!allPlayers.Contains(ph))
            allPlayers.Add(ph);
    }
}
