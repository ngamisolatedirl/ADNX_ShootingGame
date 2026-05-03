using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button choiButton;
    public Button cuaHangButton;
    public Button caiDatButton;
    public Button creditsButton;
    //public Button GunShopButton;
    //public Button CharactersShopButton;
    //public Button CostumesShopButton;
    //public Button UpgradesShopButton;
    //public Button BackButton;
    void Start()
    {
        
        
    }

    //public void OpenPlay() => SceneManager.LoadScene("LevelSelect");
    public void OpenPlay()
    {
        SceneManager.LoadScene("LevelSelect");
    }
    //    public void OpenShop() => Debug.Log("Cua hang chua co!");
    public void OpenSettings()
    {
        SceneManager.LoadScene("Options");
    }
    public void OpenCredits()
    {
        SceneManager.LoadScene("Credits");
    }
    public void OpenShop()
    {
        SceneManager.LoadScene("Shop");
    }
    //public void OpenGunShop()
    //{
    //    SceneManager.LoadScene("ShopGunsScene");
    //}
    //public void BackMenu()
    //{
    //    SceneManager.LoadScene("MainMenu");
    //}

}