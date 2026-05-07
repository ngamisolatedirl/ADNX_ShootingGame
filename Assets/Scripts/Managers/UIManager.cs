using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Health Bar")]
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    [Header("Kill / Coin")]
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI coinsText;

    [Header("Spectate")]
    public GameObject spectateLabel;        // "Đang xem: PlayerX"
    public TextMeshProUGUI spectateLabelText;

    [Header("Win Zone Waiting")]
    public GameObject waitingTeammatesUI;   // "Chờ đồng đội... (1/2)"
    public TextMeshProUGUI waitingText;

    private PlayerHealth playerHealth;
    private int killCount = 0;

    void Start()
    {
        // Tìm player của local client
        if (NetworkUtils.IsOnline)
        {
            // Tìm PlayerHealth là owner
            foreach (var ph in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                var nb = ph.GetComponent<Unity.Netcode.NetworkBehaviour>();
                if (nb != null && nb.IsOwner) { playerHealth = ph; break; }
            }
        }
        else
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        spectateLabel?.SetActive(false);
        waitingTeammatesUI?.SetActive(false);
    }

    void Update()
    {
        UpdateHealthBar();
        UpdateCoins();
    }

    void UpdateHealthBar()
    {
        if (playerHealth == null) return;
        float ratio = playerHealth.GetHealth() / playerHealth.maxHealth;
        healthBarFill.fillAmount = ratio;
        if (healthText != null)
            healthText.text = $"{(int)playerHealth.GetHealth()} / {(int)playerHealth.maxHealth}";
    }

    public void AddKill()
    {
        killCount++;
        if (killCountText != null)
            killCountText.text = "Kills: " + killCount;
    }

    void UpdateCoins()
    {
        if (coinsText == null) return;
        int coins = SessionData.Instance != null
            ? SessionData.Instance.sessionCoins
            : (DataManager.Instance?.GetCoins() ?? 0);
        coinsText.text = "Coins: " + coins;
    }

    // ── Spectate UI ────────────────────────────────────────────────────────

    public void ShowSpectating(string targetName)
    {
        if (spectateLabel == null) return;
        spectateLabel.SetActive(true);
        if (spectateLabelText != null)
            spectateLabelText.text = $"Đang xem: {targetName}";
    }

    public void HideSpectating()
    {
        spectateLabel?.SetActive(false);
    }

    // ── Waiting UI ─────────────────────────────────────────────────────────

    public void ShowWaiting(int inZone, int total)
    {
        if (waitingTeammatesUI == null) return;
        waitingTeammatesUI.SetActive(inZone < total);
        if (waitingText != null)
            waitingText.text = $"Chờ đồng đội... ({inZone}/{total})";
    }

    public void HideWaiting()
    {
        waitingTeammatesUI?.SetActive(false);
    }
}
