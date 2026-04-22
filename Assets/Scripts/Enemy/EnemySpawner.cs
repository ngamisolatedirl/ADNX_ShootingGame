using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int maxEnemies = 5;
    public float spawnInterval = 3f;
    public float spawnY = -2f;
    public float spawnDistance = 10f; 

    private float timer = 0f;
    private int currentEnemies = 0;
    private Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval && currentEnemies < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Chưa gán Enemy Prefab!");
            return;
        }

        if (player == null) return;

        // Spawn bên trái hoặc bên phải player với khoảng cách spawnDistance
        float randomX;
        if (Random.value > 0.5f)
            randomX = player.position.x + spawnDistance;  // Bên phải player
        else
            randomX = player.position.x - spawnDistance;  // Bên trái player

        Vector3 spawnPos = new Vector3(randomX, spawnY, 0);
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        currentEnemies++;

        enemy.GetComponent<Enemy>().OnDeath += () => currentEnemies--;
    }
}