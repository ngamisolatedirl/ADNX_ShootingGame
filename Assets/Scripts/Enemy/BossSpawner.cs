using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Boss Settings")]
    public GameObject bossPrefab;          // Prefab Boss
    public Transform spawnPoint;           // Vị trí spawn Boss
    public GameObject winZonePrefab;       // Prefab WinZone
    public Transform winZoneSpawnPoint;    // Vị trí spawn WinZone

    private bool bossSpawned = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!bossSpawned && collision.CompareTag("Player"))
        {
            SpawnBoss();
            bossSpawned = true;
        }
    }

    void SpawnBoss()
    {
        GameObject boss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);

        // Gán WinZone cho Boss
        BossEnemy bossScript = boss.GetComponent<BossEnemy>();
        bossScript.winZonePrefab = winZonePrefab;
        bossScript.winZoneSpawnPoint = winZoneSpawnPoint;
    }
}
