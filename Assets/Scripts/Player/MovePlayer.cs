using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class MovePlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public LayerMask groundLayer;

    [Header("Charge Jump")]
    public float minChargeJump = 10f;
    public float maxChargeJump = 30f;
    public float maxChargeTime = 1.5f;

    [Header("Wall Detection")]
    public float wallRayDistance = 0.55f;

    [Header("One Way Platform")]
    public LayerMask oneWayLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private Collider2D oneWayCollider;
    private bool isFacingRight = true;
    private float rayDistance = 1.7f;
    private float chargeTimer = 0f;
    private bool isCharging = false;

    // --- Ground Check ---
    bool isGrounded =>
        Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundLayer | oneWayLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(0.4f, 0, 0), Vector2.down, rayDistance, groundLayer | oneWayLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(-0.4f, 0, 0), Vector2.down, rayDistance, groundLayer | oneWayLayer);

    bool IsHittingWall(float inputX)
    {
        if (inputX == 0) return false;
        Vector2 dir = inputX > 0 ? Vector2.right : Vector2.left;
        float checkOffset = col != null ? col.size.y * 0.3f : 0.5f;
        return Physics2D.Raycast(transform.position + new Vector3(0, checkOffset, 0), dir, wallRayDistance, groundLayer) ||
               Physics2D.Raycast(transform.position - new Vector3(0, checkOffset, 0), dir, wallRayDistance, groundLayer);
    }

    public override void OnNetworkSpawn()
    {
        Initialize();

        if (NetworkUtils.IsOnline)
        {
            if (IsOwner)
            {
                // Owner: Dynamic → tự tính vật lý
                rb.bodyType = RigidbodyType2D.Dynamic;

                // Camera được setup bởi PlayerCameraController trên prefab
                // KHÔNG gọi SetupLocalCamera ở đây nữa
            }
            else
            {
                // Non-owner: Kinematic → NetworkTransform (Client Authority) điều khiển vị trí
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }
        }
        else
        {
            // Offline
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void Start()
    {
        // Fallback cho Offline nếu OnNetworkSpawn chưa chạy
        if (!NetworkUtils.IsOnline) Initialize();
    }

    private void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"));

        GameObject oneWay = GameObject.Find("Tilemap_OneWay");
        if (oneWay != null) oneWayCollider = oneWay.GetComponent<Collider2D>();
    }

    void Update()
    {
        // Chỉ owner xử lý input
        if (NetworkUtils.IsOnline && !IsOwner) return;

        Move();
        ChargeJump();
        HandleOneWayPlatform();
    }

    void Move()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (isCharging)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float moveInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput = 1f;

        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();

        float finalMoveX = IsHittingWall(moveInput) ? 0f : moveInput;

        if (isGrounded)
            rb.linearVelocity = new Vector2(finalMoveX * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, finalMoveX * moveSpeed, 0.1f), rb.linearVelocity.y);

        if (keyboard.upArrowKey.wasPressedThisFrame && isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (rb.linearVelocity.y < 0) rb.gravityScale = 4f;
        else if (rb.linearVelocity.y > 0) rb.gravityScale = 2f;
        else rb.gravityScale = 1f;
    }

    void ChargeJump()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.vKey.isPressed && isGrounded)
        {
            isCharging = true;
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0, maxChargeTime);
        }

        if (keyboard.vKey.wasReleasedThisFrame && isCharging && isGrounded)
        {
            float chargeRatio = chargeTimer / maxChargeTime;
            float force = Mathf.Lerp(minChargeJump, maxChargeJump, chargeRatio);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            chargeTimer = 0f;
            isCharging = false;
        }

        if (!isGrounded && isCharging)
        {
            chargeTimer = 0f;
            isCharging = false;
        }
    }

    void HandleOneWayPlatform()
    {
        if (oneWayCollider == null || col == null) return;
        bool playerBelowPlatform = col.bounds.max.y < oneWayCollider.bounds.min.y;
        bool playerAbovePlatform = col.bounds.min.y >= oneWayCollider.bounds.min.y;

        if (rb.linearVelocity.y > 0 && playerBelowPlatform)
            Physics2D.IgnoreCollision(col, oneWayCollider, true);
        else if (playerAbovePlatform)
            Physics2D.IgnoreCollision(col, oneWayCollider, false);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public bool IsGrounded() => isGrounded;
    public bool IsCharging() => isCharging;
    public float GetChargeRatio() => chargeTimer / maxChargeTime;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * rayDistance);
        Gizmos.color = Color.blue;
        Vector3 dir = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position + new Vector3(0, 0.5f, 0), dir * wallRayDistance);
    }
}