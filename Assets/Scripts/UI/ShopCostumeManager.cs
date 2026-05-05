using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopCostumeManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterColumn
    {
        public string characterId;
        public TextMeshProUGUI characterNameText;
        public RectTransform[] costumeSlots; 
    }

    public CharacterColumn[] columns;
    public GameObject costumeItemPrefab;
    public TextMeshProUGUI coinsText;

    void Start()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (CharacterColumn col in columns)
        {
            // Xóa item cũ
            foreach (RectTransform slot in col.costumeSlots)
                foreach (Transform child in slot)
                    Destroy(child.gameObject);

            CharacterData config = DataManager.Instance.GetCharacterData(col.characterId);
            if (config == null) continue;

            if (col.characterNameText != null)
                col.characterNameText.text = config.name;

            for (int i = 0; i < config.costumes.Count && i < col.costumeSlots.Length; i++)
            {
                GameObject item = Instantiate(costumeItemPrefab, col.costumeSlots[i]);
                RectTransform rt = item.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                item.GetComponent<CostumeItemUI>().Setup(col.characterId, config.costumes[i]);
            }
        }

        UpdateCoins();
    }

    public void UpdateCoins()
    {
        if (coinsText != null)
            coinsText.text = "🪙 " + DataManager.Instance.GetCoins();
    }

    public void Back()
    {
        SceneManager.LoadScene("Shop");
    }
}