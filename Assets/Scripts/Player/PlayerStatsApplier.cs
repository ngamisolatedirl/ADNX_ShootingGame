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
        if (DataManager.Instance == null) return;

        BaseStats stats = DataManager.Instance.GetComputedStats(
            DataManager.Instance.GetSaveData().activeCharacterId
        );

        if (stats == null) return;

        // Apply HP
        if (playerHealth != null)
        {
            playerHealth.maxHealth = stats.hp;
            playerHealth.ResetHealth(); // Cần thêm hàm này vào PlayerHealth
        }

        // Apply Speed
        if (movePlayer != null)
            movePlayer.moveSpeed = stats.speed;

        Debug.Log($"Applied stats → HP: {stats.hp} | Speed: {stats.speed} | CritRate: {stats.critRate} | CritDmg: {stats.critDamage}");
    }
}