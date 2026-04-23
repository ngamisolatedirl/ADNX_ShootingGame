using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private MovePlayer movePlayer;

    private float idleTimer = 0f;
    public float idleDelay = 0.5f; // Thời gian chờ trước khi về Idle

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movePlayer = GetComponent<MovePlayer>();
    }

    void Update()
    {
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        bool grounded = movePlayer.IsGrounded();
        bool animGrounded = grounded && rb.linearVelocity.y <= 0.1f;

        // Delay về Idle chỉ khi không chạy
        if (isRunning)
            idleTimer = idleDelay;
        else
            idleTimer -= Time.deltaTime;

        // Nếu đang giữ nút chạy + chạm đất → Run luôn không về Idle
        bool showRunning = isRunning || idleTimer > 0;

        animator.SetBool("isRunning", showRunning);
        animator.SetBool("isGrounded", animGrounded);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    public void PlayShoot()
    {
        animator.SetTrigger("isShooting");
    }

    public void PlayDeath()
    {
        animator.SetBool("isDead", true);
    }
}