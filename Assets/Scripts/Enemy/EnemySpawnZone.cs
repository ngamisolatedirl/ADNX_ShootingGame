using UnityEngine;

public class EnemySpawnZone : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;
    public int maxEnemies = 5;
    public float spawnInterval = 3f;

    [Header("Spawn Area")]
    public Transform areaStart;
    public Transform areaEnd;
    public float spawnY = -2f;

    [Header("Activation Settings")]
    public bool activateOnPlayerEnter = false;
    public bool deactivateOnPlayerExit = false;
    public bool triggerOnlyOnce = false;    // Chỉ trigger 1 lần cả ván
    public bool spawnOnStart = false;       // Spawn ngay từ đầu trong vùng

    private float timer = 0f;
    private int currentEnemies = 0;
    private bool isActive = true;
    private bool hasTriggered = false;      // Đã trigger chưa

    void Start()
    {
        if (activateOnPlayerEnter)
            isActive = false;

        // Spawn ngay từ đầu nếu bật
        if (spawnOnStart)
        {
            for (int i = 0; i < maxEnemies; i++)
                SpawnEnemy();
        }
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval && currentEnemies < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        float randomX = Random.Range(areaStart.position.x, areaEnd.position.x);
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        currentEnemies++;

        // Thử lấy Enemy, Enemy2 hoặc MeleeEnemy
        Enemy e = enemy.GetComponent<Enemy>();
        Enemy2 e2 = enemy.GetComponent<Enemy2>();
        MeleeEnemy me = enemy.GetComponent<MeleeEnemy>();

        if (e != null) e.OnDeath += () => currentEnemies--;
        else if (e2 != null) e2.OnDeath += () => currentEnemies--;
        else if (me != null) me.OnDeath += () => currentEnemies--;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Nếu chỉ trigger 1 lần và đã trigger rồi → bỏ qua
        if (triggerOnlyOnce && hasTriggered) return;

        if (activateOnPlayerEnter)
        {
            isActive = true;
            hasTriggered = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (deactivateOnPlayerExit)
        {
            isActive = false;

            // Nếu trigger liên tục → reset để có thể trigger lại
            if (!triggerOnlyOnce)
                hasTriggered = false;
        }
    }

    void OnDrawGizmos()
    {
        if (areaStart != null && areaEnd != null)
        {
            Gizmos.color = isActive ? Color.green : Color.red;
            Gizmos.DrawLine(areaStart.position, areaEnd.position);
        }
    }
}