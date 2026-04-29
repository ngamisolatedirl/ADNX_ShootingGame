using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GunItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI gunName;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI priceText;
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    private GunData gunData;

    public void Setup(GunData data)
    {
        gunData = data;
        gunName.text = data.name;
        statsText.text = $"DMG: {data.damage} | Rate: {data.fireRate}";
        priceText.text = data.price == 0 ? "Miễn phí" : $"🪙 {data.price}";

        UpdateButton();
        actionButton.onClick.AddListener(OnButtonClick);
    }

    void UpdateButton()
    {
        SaveData save = DataManager.Instance.GetSaveData();

        if (save.activeGunId == gunData.id)
        {
            buttonText.text = "Đang dùng";
            actionButton.interactable = false;
        }
        else if (save.purchasedGuns.Contains(gunData.id))
        {
            buttonText.text = "Trang bị";
            actionButton.interactable = true;
        }
        else
        {
            buttonText.text = "Mua";
            actionButton.interactable = true;
        }
    }

    void OnButtonClick()
    {
        SaveData save = DataManager.Instance.GetSaveData();

        if (save.purchasedGuns.Contains(gunData.id))
        {
            // Trang bị
            EquipGun();
        }
        else
        {
            // Mua
            BuyGun();
        }

        UpdateButton();
    }

    void BuyGun()
    {
        if (!DataManager.Instance.SpendCoins(gunData.price))
        {
            Debug.Log("Không đủ coins!");
            return;
        }

        SaveData save = DataManager.Instance.GetSaveData();
        save.purchasedGuns.Add(gunData.id);
        DataManager.Instance.SaveGame();

        Debug.Log("Đã mua: " + gunData.name);

        // Tự động trang bị luôn sau khi mua
        EquipGun();
    }

    void EquipGun()
    {
        SaveData save = DataManager.Instance.GetSaveData();
        save.activeGunId = gunData.id;
        DataManager.Instance.SaveGame();

        Debug.Log("Đã trang bị: " + gunData.name);

        // Refresh toàn bộ list để update button
        FindFirstObjectByType<ShopGunsManager>().RefreshList();
    }
}