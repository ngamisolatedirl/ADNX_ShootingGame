using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Health Bar")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    [Header("Kill Count")]
    public TextMeshProUGUI killCountText;

    private PlayerHealth playerHealth;
    private int killCount = 0;

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void Update()
    {
        UpdateHealthBar();
    }

    
    void UpdateHealthBar()
    {
        if (playerHealth == null) return;
        float ratio = playerHealth.GetHealth() / playerHealth.maxHealth;
        healthBarFill.fillAmount = ratio;
        // bỏ dòng healthText
    }

    public void AddKill()
    {
        killCount++;
        killCountText.text = "Kills: " + killCount;
    }
}