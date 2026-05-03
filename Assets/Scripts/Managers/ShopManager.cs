using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class ShopManager : MonoBehaviour
{
    [Header("Shop Buttons")]
    public Button GunShopButton;
    public Button CharactersShopButton;
    public Button CostumesShopButton;
    public Button UpgradesShopButton;
    public Button BackButton;
    public Button BackShopButton;

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
}
