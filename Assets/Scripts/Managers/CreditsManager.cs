using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CreditsScene : MonoBehaviour
{
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            BackToMenu();
    }
}