using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;

    [Header("Lock Text")]
    public TextMeshProUGUI level2Text;
    public TextMeshProUGUI level3Text;

    void Start()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        // Level 2
        if (unlockedLevel >= 2)
        {
            level2Button.interactable = true;
            level2Text.text = "Level 2";
        }
        else
        {
            level2Button.interactable = false;
            level2Text.text = "Level 2 🔒";
        }

        // Level 3
        if (unlockedLevel >= 3)
        {
            level3Button.interactable = true;
            level3Text.text = "Level 3";
        }
        else
        {
            level3Button.interactable = false;
            level3Text.text = "Level 3 🔒";
        }
    }

    public void LoadLevel1() => SceneManager.LoadScene("Level1");
    public void LoadLevel2() => SceneManager.LoadScene("Level2");
    public void LoadLevel3() => SceneManager.LoadScene("Level3");
    public void BackToMenu() => SceneManager.LoadScene("MainMenu");
}