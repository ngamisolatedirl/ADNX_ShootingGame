using UnityEngine;
using Unity.Netcode;

public class FireWall : NetworkBehaviour
{
    [Header("Movement")]
    public Transform startPoint;
    public Transform endPoint;
    public float moveSpeed = 2f;

    [Header("Damage")]
    public float damagePerSecond = 50f;

    private NetworkVariable<float> networkPositionX = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool movingForward = true;
    private bool reachedEnd = false;

    // Track player đang chạm để DoT
    private System.Collections.Generic.HashSet<PlayerHealth> touchingPlayers
        = new System.Collections.Generic.HashSet<PlayerHealth>();

    // ── Lifecycle ──────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (IsServer && startPoint != null)
        {
            networkPositionX.Value = startPoint.position.x;
            transform.position = new Vector3(startPoint.position.x, transform.position.y, transform.position.z);
        }

        networkPositionX.OnValueChanged += OnPositionChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkPositionX.OnValueChanged -= OnPositionChanged;
    }

    void OnPositionChanged(float oldVal, float newVal)
    {
        // Client cập nhật vị trí theo server
        transform.position = new Vector3(newVal, transform.position.y, transform.position.z);
    }

    // ── Update ─────────────────────────────────────────────────────

    void Update()
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (reachedEnd) return;
        if (startPoint == null || endPoint == null) return;

        float targetX = movingForward ? endPoint.position.x : startPoint.position.x;
        float currentX = transform.position.x;
        float diff = targetX - currentX;

        if (Mathf.Abs(diff) < 0.05f)
        {
            reachedEnd = true;
            return;
        }

        float newX = currentX + Mathf.Sign(diff) * moveSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        networkPositionX.Value = newX;

        // DoT cho các player đang chạm
        foreach (var player in touchingPlayers)
        {
            if (player != null)
                player.TakeDamageFromServer(damagePerSecond * Time.deltaTime);
        }
    }

    // ── Trigger ────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null) touchingPlayers.Add(ph);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null) touchingPlayers.Remove(ph);
        }
    }

    // ── Gizmos ─────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
        }
    }
}