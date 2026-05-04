using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterColumn
    {
        public string characterId;
        public TextMeshProUGUI characterNameText;
        public RectTransform[] slots; // 4 slot: hp, speed, critRate, critDamage
    }

    public CharacterColumn[] columns; // 2 column, assign trong Inspector
    public TextMeshProUGUI coinsText;
    public GameObject statUpgradePrefab;

    private string[] statTypes = { "hp", "speed", "critRate", "critDamage" };
    private StatUpgradeItemUI[][] items;

    void Start()
    {
        items = new StatUpgradeItemUI[columns.Length][];

        for (int c = 0; c < columns.Length; c++)
        {
            CharacterColumn col = columns[c];
            items[c] = new StatUpgradeItemUI[statTypes.Length];

            if (col.characterNameText != null)
            {
                CharacterData config = DataManager.Instance.GetCharacterData(col.characterId);
                col.characterNameText.text = config != null ? config.name : col.characterId;
            }

            for (int i = 0; i < statTypes.Length && i < col.slots.Length; i++)
            {
                GameObject item = Instantiate(statUpgradePrefab, col.slots[i]);
                RectTransform rt = item.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                items[c][i] = item.GetComponent<StatUpgradeItemUI>();
                items[c][i].Setup(col.characterId, statTypes[i]);
            }
        }

        UpdateCoins();
    }

    public void UpdateCoins()
    {
        if (coinsText != null)
            coinsText.text = "🪙 " + DataManager.Instance.GetCoins();

        if (items == null) return;
        foreach (var col in items)
            foreach (var item in col)
                if (item != null) item.Refresh();
    }

    public void Back()
    {
        SceneManager.LoadScene("Shop");
    }
}