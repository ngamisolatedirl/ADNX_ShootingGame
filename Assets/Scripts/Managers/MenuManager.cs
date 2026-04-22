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
        if (caiDatButton != null) caiDatButton.interactable = false;
        if (creditsButton != null) creditsButton.interactable = false;
    }

    //public void OpenPlay() => SceneManager.LoadScene("LevelSelect");
    public void OpenPlay()
    {
        SceneManager.LoadScene("LevelSelect");
    }
//    public void OpenShop() => Debug.Log("Cua hang chua co!");
//    public void OpenSettings() => Debug.Log("Cai dat chua co!");
//    public void OpenCredits() => Debug.Log("Credits chua co!");
  }