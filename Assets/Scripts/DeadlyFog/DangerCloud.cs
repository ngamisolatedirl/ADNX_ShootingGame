using UnityEngine;

public class DangerCloud : MonoBehaviour
{
    [Header("Spread")]
    public float startLifetime = 1f;        // Lifetime ban đầu
    public float maxLifetime = 6f;          // Lifetime tối đa
    public float lifetimeIncreaseRate = 0.3f; // Tăng lifetime mỗi giây

    [Header("Particle Speed")]
    public float particleSpeed = 3f;        // Tốc độ bay sang phải

    [Header("Kill Zone")]
    public float cloudReachX = 0f;          // X mà cloud đã lan tới
    public float dangerWidth = 1f;          // Độ rộng vùng kill

    private ParticleSystem ps;
    private float currentLifetime;
    private Transform player;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        player = GameObject.FindWithTag("Player").transform;
        currentLifetime = startLifetime;
    }

    void Update()
    {
        // Tăng lifetime → particle bay xa hơn theo thời gian
        currentLifetime += lifetimeIncreaseRate * Time.deltaTime;
        currentLifetime = Mathf.Min(currentLifetime, maxLifetime);

        var main = ps.main;
        main.startLifetime = currentLifetime;

        // Tính mép phải của cloud đang lan tới
        cloudReachX = transform.position.x + particleSpeed * currentLifetime;

        // Kiểm tra player có trong vùng cloud không
        if (player != null && player.position.x <= cloudReachX)
        {
            player.GetComponent<PlayerHealth>()?.Die();
        }
    }
}