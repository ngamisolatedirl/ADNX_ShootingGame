using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI priceText;
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    private CharacterData characterData;

    public void Setup(CharacterData data)
    {
        characterData = data;
        characterName.text = data.name;

        BaseStats stats = DataManager.Instance.GetComputedStats(data.id);
        statsText.text = $"HP: {stats.hp} | SPD: {stats.speed} | CRIT: {stats.critRate * 100}%";
        priceText.text = data.price == 0 ? "Miễn phí" : $"🪙 {data.price}";

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClick);

        UpdateButton();
    }

    void UpdateButton()
    {
        CharacterSaveData save = DataManager.Instance.GetCharacterSaveData(characterData.id);

        if (DataManager.Instance.GetSaveData().activeCharacterId == characterData.id)
        {
            buttonText.text = "Đang dùng";
            actionButton.interactable = false;
        }
        else if (save.isPurchased)
        {
            buttonText.text = "Trang bị";
            actionButton.interactable = true;
        }
        else
        {
            buttonText.text = $"Mua 🪙{characterData.price}";
            actionButton.interactable = true;
        }
    }

    void OnButtonClick()
    {
        CharacterSaveData save = DataManager.Instance.GetCharacterSaveData(characterData.id);

        if (save.isPurchased)
        {
            DataManager.Instance.EquipCharacter(characterData.id);
        }
        else
        {
            if (!DataManager.Instance.SpendCoins(characterData.price))
            {
                Debug.Log("Không đủ coins!");
                return;
            }
            DataManager.Instance.PurchaseCharacter(characterData.id);
            DataManager.Instance.EquipCharacter(characterData.id);
        }

        FindFirstObjectByType<ShopCharactersManager>().RefreshList();
    }
}