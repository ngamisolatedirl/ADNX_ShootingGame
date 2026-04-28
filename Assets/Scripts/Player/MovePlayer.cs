using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public LayerMask groundLayer;

    [Header("Charge Jump")]
    public float minChargeJump = 10f;
    public float maxChargeJump = 30f;
    public float maxChargeTime = 1.5f;

    [Header("Wall Detection")]
    public float wallRayDistance = 0.55f; // Khoảng cách check tường từ tâm nhân vật

    private Rigidbody2D rb;
    private CapsuleCollider2D col; // Thêm để lấy thông tin chiều cao
    private bool isFacingRight = true;
    private float rayDistance = 1.7f;
    private float chargeTimer = 0f;
    private bool isCharging = false;

    bool isGrounded =>
        Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(0.4f, 0, 0), Vector2.down, rayDistance, groundLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(-0.4f, 0, 0), Vector2.down, rayDistance, groundLayer);

    // Hàm kiểm tra xem có đang đâm đầu vào tường không
    bool IsHittingWall(float inputX)
    {
        if (inputX == 0) return false;

        Vector2 dir = inputX > 0 ? Vector2.right : Vector2.left;
        // Bắn 2 tia (trên và dưới) để check tường chắc chắn hơn
        float checkOffset = col != null ? col.size.y * 0.3f : 0.5f;

        return Physics2D.Raycast(transform.position + new Vector3(0, checkOffset, 0), dir, wallRayDistance, groundLayer) ||
               Physics2D.Raycast(transform.position - new Vector3(0, checkOffset, 0), dir, wallRayDistance, groundLayer);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),
            LayerMask.NameToLayer("Enemy")
        );
    }

    void Update()
    {
        Move();
        ChargeJump();
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
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            moveInput = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            moveInput = 1f;

        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();

        // --- PHẦN CHỐNG DÍNH TƯỜNG ---
        float finalMoveX = moveInput;
        if (IsHittingWall(moveInput))
        {
            finalMoveX = 0f; // Triệt tiêu lực ép ngang nếu có tường
        }
        // -----------------------------

        // Giữ nguyên logic di chuyển cũ nhưng thay moveInput bằng finalMoveX
        if (isGrounded)
            rb.linearVelocity = new Vector2(finalMoveX * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, finalMoveX * moveSpeed, 0.1f),
                rb.linearVelocity.y
            );

        if (keyboard.upArrowKey.wasPressedThisFrame && isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (rb.linearVelocity.y < 0)
            rb.gravityScale = 4f;
        else if (rb.linearVelocity.y > 0)
            rb.gravityScale = 2f;
        else
            rb.gravityScale = 1f;
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
        Gizmos.DrawRay(transform.position + new Vector3(0.4f, 0, 0), Vector2.down * rayDistance);
        Gizmos.DrawRay(transform.position + new Vector3(-0.4f, 0, 0), Vector2.down * rayDistance);

        // Vẽ tia check tường để bạn dễ căn chỉnh wallRayDistance trong Scene
        Gizmos.color = Color.blue;
        Vector3 dir = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position + new Vector3(0, 0.5f, 0), dir * wallRayDistance);
        Gizmos.DrawRay(transform.position - new Vector3(0, 0.5f, 0), dir * wallRayDistance);
    }
}