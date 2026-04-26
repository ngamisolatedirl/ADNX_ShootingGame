using UnityEngine;

public class DangerWall : MonoBehaviour
{
    [Header("Movement")]
    public float startSpeed = 1f;
    public float acceleration = 0.1f;
    public float maxSpeed = 6f;

    [Header("Delay")]
    public float startDelay = 5f;        // Chờ bao nhiêu giây mới bắt đầu di chuyển

    private float currentSpeed = 0f;
    private bool isMoving = false;
    private float delayTimer = 0f;

    void Update()
    {
        if (!isMoving)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= startDelay)
            {
                isMoving = true;
                currentSpeed = startSpeed;
            }
            return;
        }

        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        transform.position += new Vector3(currentSpeed * Time.deltaTime, 0, 0);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger hit: " + collision.gameObject.name + " tag: " + collision.tag);

        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerHealth>()?.Die();
        }
    }
}