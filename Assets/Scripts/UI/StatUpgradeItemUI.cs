using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatUpgradeItemUI : MonoBehaviour
{
    public TextMeshProUGUI statName;
    public TextMeshProUGUI statValue;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public Button upgradeButton;
    public TextMeshProUGUI buttonText;

    private string characterId;
    private string statType;

    public void Setup(string characterId, string statType)
    {
        this.characterId = characterId;
        this.statType = statType;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeClick);

        Refresh();
    }

    public void Refresh()
    {
        BaseStats stats = DataManager.Instance.GetComputedStats(characterId);
        CharacterData config = DataManager.Instance.GetCharacterData(characterId);
        int level = DataManager.Instance.GetUpgradeLevel(characterId, statType);
        int cost = DataManager.Instance.GetUpgradeCost(characterId, statType);
        int maxLevel = 0;

        switch (statType)
        {
            case "hp":
                statName.text = "❤️ HP";
                statValue.text = $"{stats.hp}";
                maxLevel = config.upgrades.hp.maxLevel;
                break;
            case "speed":
                statName.text = "👟 Speed";
                statValue.text = $"{stats.speed:F1}";
                maxLevel = config.upgrades.speed.maxLevel;
                break;
            case "critRate":
                statName.text = "🎯 Crit Rate";
                statValue.text = $"{stats.critRate * 100:F1}%";
                maxLevel = config.upgrades.critRate.maxLevel;
                break;
            case "critDamage":
                statName.text = "💥 Crit Dmg";
                statValue.text = $"{stats.critDamage:F2}x";
                maxLevel = config.upgrades.critDamage.maxLevel;
                break;
        }

        levelText.text = $"Lv {level}/{maxLevel}";

        if (cost == -1)
        {
            costText.text = "MAX";
            upgradeButton.interactable = false;
            buttonText.text = "MAX";
        }
        else
        {
            costText.text = $"🪙 {cost}";
            upgradeButton.interactable = DataManager.Instance.GetCoins() >= cost;
            buttonText.text = "Upgrade";
        }
    }

    void OnUpgradeClick()
    {
        bool success = DataManager.Instance.UpgradeStat(characterId, statType);
        if (!success)
        {
            Debug.Log("Upgrade thất bại!");
            return;
        }

        Refresh();
        FindFirstObjectByType<UpgradeManager>().UpdateCoins();
    }
}