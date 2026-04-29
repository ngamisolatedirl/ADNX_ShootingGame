using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopGunsManager : MonoBehaviour
{
    public Transform content;           // Content trong ScrollView
    public GameObject gunItemPrefab;    // Prefab GunItem
    public TextMeshProUGUI coinsText;

    void Start()
    {
        RefreshList();
        UpdateCoins();
    }

    public void RefreshList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        GunConfig config = DataManager.Instance.GetGunConfig();
        Debug.Log("Số súng: " + config.guns.Count);

        foreach (GunData gun in config.guns)
        {
            Debug.Log("Spawn item: " + gun.name);
            GameObject item = Instantiate(gunItemPrefab, content);
            GunItemUI ui = item.GetComponent<GunItemUI>();
            Debug.Log("GunItemUI: " + ui);
            ui.Setup(gun);
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