using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình chọn level (Offline).
/// Attach vào LevelSelectScene.
/// Level unlock theo PlayerPrefs "UnlockedLevel".
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Buttons")]
    public Button[] levelButtons;           // 4 nút Level 1~4
    public string[] levelSceneNames = { "Level1", "Level2", "Level3", "Level4" };

    [Header("UI")]
    public Button backButton;
    public TextMeshProUGUI titleText;

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        DataManager.EnsureExists();

        if (SessionData.Instance == null)
        {
            var go = new GameObject("SessionData");
            go.AddComponent<SessionData>();
        }

        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            bool isUnlocked = (i + 1) <= unlockedLevel;

            levelButtons[i].interactable = isUnlocked;
            levelButtons[i].onClick.AddListener(() => LoadLevel(idx));

            // Visual feedback: lock icon hoặc text
            var labelText = levelButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
                labelText.text = isUnlocked
                    ? $"Level {i + 1}"
                    : $"Level {i + 1} 🔒";
        }

        backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));
    }

    void LoadLevel(int index)
    {
        if (index >= levelSceneNames.Length) return;
        SessionData.Instance?.Reset();
        SceneManager.LoadScene(levelSceneNames[index]);
    }
}
