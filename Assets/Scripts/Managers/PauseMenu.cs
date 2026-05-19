using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (!NetworkUtils.IsOnline)
            Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        if (!NetworkUtils.IsOnline)
            Time.timeScale = 0f;
        isPaused = true;
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;

        if (NetworkUtils.IsOnline)
        {
            // Uỷ cho GameManager xử lý: báo server → kick → shutdown → load scene
            if (GameManager.Instance != null)
                GameManager.Instance.ClientLeaveToMenu();
            else
            {
                // Fallback nếu GameManager chưa spawn
                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsListening)
                    Unity.Netcode.NetworkManager.Singleton.Shutdown();

                RoomContext.Clear();
                SceneManager.LoadScene("MainMenu");
            }
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}