using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopGunsManager : MonoBehaviour
{
    public RectTransform[] slots;       // 4 slot, assign trong Inspector
    public GameObject gunItemPrefab;
    public TextMeshProUGUI coinsText;

    void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        // Xóa item cũ trong slot
        foreach (RectTransform slot in slots)
        {
            foreach (Transform child in slot)
                Destroy(child.gameObject);
        }

        GunConfig config = DataManager.Instance.GetGunConfig();

        for (int i = 0; i < config.guns.Count && i < slots.Length; i++)
        {
            GameObject item = Instantiate(gunItemPrefab, slots[i]);
            RectTransform rt = item.GetComponent<RectTransform>();

            // Stretch full slot
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            item.GetComponent<GunItemUI>().Setup(config.guns[i]);
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