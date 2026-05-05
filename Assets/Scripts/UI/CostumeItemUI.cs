using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CostumeItemUI : MonoBehaviour
{
    public TextMeshProUGUI costumeName;
    public TextMeshProUGUI priceText;
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    private string characterId;
    private CostumeData costumeData;

    public void Setup(string characterId, CostumeData data)
    {
        this.characterId = characterId;
        costumeData = data;

        costumeName.text = data.name;
        priceText.text = data.price == 0 ? "Miễn phí" : $"🪙 {data.price}";

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClick);

        Refresh();
    }

    public void Refresh()
    {
        if (DataManager.Instance.IsCostumeActive(characterId, costumeData.id))
        {
            buttonText.text = "Đang dùng";
            actionButton.interactable = false;
        }
        else if (DataManager.Instance.IsCostumePurchased(characterId, costumeData.id))
        {
            buttonText.text = "Trang bị";
            actionButton.interactable = true;
        }
        else
        {
            buttonText.text = $"Mua 🪙{costumeData.price}";
            actionButton.interactable = true;
        }
    }

    void OnButtonClick()
    {
        if (DataManager.Instance.IsCostumePurchased(characterId, costumeData.id))
        {
            DataManager.Instance.EquipCostume(characterId, costumeData.id);
        }
        else
        {
            if (!DataManager.Instance.SpendCoins(costumeData.price))
            {
                Debug.Log("Không đủ coins!");
                return;
            }
            DataManager.Instance.PurchaseCostume(characterId, costumeData.id);
            DataManager.Instance.EquipCostume(characterId, costumeData.id);
        }

        FindFirstObjectByType<ShopCostumeManager>().RefreshAll();
    }
}