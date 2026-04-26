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

    void Start()
    {
        // Cửa hàng, Cài đặt, Credits chưa làm nên tạm disable
        if (cuaHangButton != null) cuaHangButton.interactable = false;
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
}