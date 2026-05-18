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
    public Button lobbyButton;
    
    void Start()
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;
        if (choiButton != null) choiButton.interactable = !isLoggedIn;
        if (lobbyButton != null) lobbyButton.interactable = isLoggedIn;

    }


    public void OpenPlay()
    {

        SceneManager.LoadScene("LevelSelect");
    }
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
    public void OpenLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // dừng play mode trong Editor
#else
    Application.Quit(); // thoát build thật
#endif
    }

}