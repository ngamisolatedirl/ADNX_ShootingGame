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

    private float timer = 0f;
    private int currentEnemies = 0;
    private bool isActive = true;

    void Start()
    {
        // Nếu chọn activateOnPlayerEnter thì tắt spawner từ đầu
        if (activateOnPlayerEnter)
            isActive = false;
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
        enemy.GetComponent<Enemy>().OnDeath += () => currentEnemies--;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Player bước vào → bật spawner
        if (activateOnPlayerEnter)
            isActive = true;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Player đi qua → tắt spawner
        if (deactivateOnPlayerExit)
            isActive = false;
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