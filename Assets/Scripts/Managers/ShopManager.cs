using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ShopManager : MonoBehaviour
{
    [Header("Shop Buttons")]
    public Button GunShopButton;
    public Button CharactersShopButton;
    public Button CostumesShopButton;
    public Button UpgradesShopButton;
    public Button BackButton;
    public Button BackShopButton;
    public TextMeshProUGUI coinsText;
    void Start()
    {
        
    }

    
    public void OpenGunShop()
    {
        SceneManager.LoadScene("ShopGunsScene");
    }
    public void BackMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void BackToShop()
    {
        SceneManager.LoadScene("Shop");
    }
    public void OpenCharacterShop()
    {
        SceneManager.LoadScene("ShopCharacters");
    }
    public void OpenUpgradesShop()
    {
        SceneManager.LoadScene("ShopUpgrades");
    }
    public void OpenCostumesShop()
    {
        SceneManager.LoadScene("ShopCostumes");
    }



    public void RefreshList()
    {
        

        CharacterConfig config = DataManager.Instance.GetCharacterConfig();
        UpdateCoins();
    }


    public void UpdateCoins()
    {
        if (coinsText != null)
            coinsText.text = "Coins : " + DataManager.Instance.GetCoins();
    }
}
