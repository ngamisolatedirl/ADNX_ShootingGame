using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình Lobby Online: chọn Host hoặc Join.
/// Attach vào LobbyScene.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button hostButton;
    public Button joinButton;
    public Button backButton;

    [Header("Scenes")]
    public string createRoomScene = "CreateRoom";
    public string roomListScene = "RoomList";
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        backButton.onClick.AddListener(OnBackClicked);

        // Đảm bảo DataManager tồn tại
        DataManager.EnsureExists();

        // Đảm bảo LanDiscovery tồn tại (singleton DontDestroyOnLoad)
        if (LanDiscovery.Instance == null)
        {
            var go = new GameObject("LanDiscovery");
            go.AddComponent<LanDiscovery>();
        }

        // Đảm bảo SessionData tồn tại
        if (SessionData.Instance == null)
        {
            var go = new GameObject("SessionData");
            go.AddComponent<SessionData>();
        }
    }

    void OnHostClicked()
    {
        SceneManager.LoadScene(createRoomScene);
    }

    void OnJoinClicked()
    {
        SceneManager.LoadScene(roomListScene);
    }

    void OnBackClicked()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
