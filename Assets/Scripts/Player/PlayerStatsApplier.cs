using UnityEngine;

public class PlayerStatsApplier : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private MovePlayer movePlayer;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        movePlayer = GetComponent<MovePlayer>();

        Apply();
    }

    public void Apply()
    {
        if (DataManager.Instance == null) { Debug.LogError("DataManager null!"); return; }

        BaseStats stats = DataManager.Instance.GetComputedStats(
            DataManager.Instance.GetSaveData().activeCharacterId
        );

        if (stats == null) { Debug.LogError("Stats null!"); return; }

        Debug.Log($"Applying speed: {stats.speed}");

        if (playerHealth != null)
        {
            playerHealth.maxHealth = stats.hp;
            playerHealth.ResetHealth();
        }

        if (movePlayer != null)
            movePlayer.moveSpeed = stats.speed;
    }
}