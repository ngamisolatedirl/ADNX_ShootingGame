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

        actionButton.onClick.RemoveAllListeners(); // tránh duplicate listener
        actionButton.onClick.AddListener(OnButtonClick);

        UpdateButton();
    }

    void UpdateButton()
    {
        if (DataManager.Instance.IsGunActive(gunData.id))
        {
            buttonText.text = "Đang dùng";
            actionButton.interactable = false;
        }
        else if (DataManager.Instance.IsGunPurchased(gunData.id))
        {
            buttonText.text = "Trang bị";
            actionButton.interactable = true;
        }
        else
        {
            buttonText.text = $"Mua 🪙{gunData.price}";
            actionButton.interactable = true;
        }
    }

    void OnButtonClick()
    {
        if (DataManager.Instance.IsGunPurchased(gunData.id))
        {
            DataManager.Instance.EquipGun(gunData.id);
        }
        else
        {
            if (!DataManager.Instance.SpendCoins(gunData.price))
            {
                Debug.Log("Không đủ coins!");
                return;
            }
            DataManager.Instance.PurchaseGun(gunData.id);
            DataManager.Instance.EquipGun(gunData.id); // tự động equip sau khi mua
        }

        FindFirstObjectByType<ShopGunsManager>().RefreshList();
    }
}