using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopCharactersManager : MonoBehaviour
{
    public RectTransform[] slots;
    public GameObject characterItemPrefab;
    public TextMeshProUGUI coinsText;

    void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        foreach (RectTransform slot in slots)
        {
            foreach (Transform child in slot)
                Destroy(child.gameObject);
        }

        CharacterConfig config = DataManager.Instance.GetCharacterConfig();

        for (int i = 0; i < config.characters.Count && i < slots.Length; i++)
        {
            GameObject item = Instantiate(characterItemPrefab, slots[i]);
            RectTransform rt = item.GetComponent<RectTransform>();

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            item.GetComponent<CharacterItemUI>().Setup(config.characters[i]);
        }

        UpdateCoins();
    }

    public void UpdateCoins()
    {
        if (coinsText != null)
            coinsText.text = "🪙 " + DataManager.Instance.GetCoins();
    }

    public void BackToShop()
    {
        SceneManager.LoadScene("Shop");
    }
}