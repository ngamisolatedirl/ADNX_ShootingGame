using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public LayerMask groundLayer;

    [Header("Charge Jump")]
    public float minChargeJump = 10f;   // Nhảy tối thiểu
    public float maxChargeJump = 30f;   // Nhảy tối đa
    public float maxChargeTime = 1.5f;  // Thời gian giữ tối đa

    private Rigidbody2D rb;
    private bool isFacingRight = true;
    private float rayDistance = 1.7f;
    private float chargeTimer = 0f;
    private bool isCharging = false;

    bool isGrounded =>
        Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(0.4f, 0, 0), Vector2.down, rayDistance, groundLayer) ||
        Physics2D.Raycast(transform.position + new Vector3(-0.4f, 0, 0), Vector2.down, rayDistance, groundLayer);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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

        // Đang charge → không di chuyển
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

        if (isGrounded)
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, moveInput * moveSpeed, 0.1f),
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

        // Bắt đầu charge khi giữ V và đang đứng trên đất
        if (keyboard.vKey.isPressed && isGrounded)
        {
            isCharging = true;
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0, maxChargeTime);
        }

        // Thả V → nhảy
        if (keyboard.vKey.wasReleasedThisFrame && isCharging && isGrounded)
        {
            float chargeRatio = chargeTimer / maxChargeTime;
            float force = Mathf.Lerp(minChargeJump, maxChargeJump, chargeRatio);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            chargeTimer = 0f;
            isCharging = false;
        }

        // Reset nếu rời đất khi đang charge
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
    }
}