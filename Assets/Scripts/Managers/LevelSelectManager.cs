using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode; // THÊM thư viện này

/// <summary>
/// Màn hình chọn level (Xử lý Offline bằng cơ chế StartHost).
/// Attach vào LevelSelectScene.
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
        // 1. Đảm bảo các Manager cốt lõi tồn tại
        DataManager.EnsureExists();

        if (SessionData.Instance == null)
        {
            var go = new GameObject("SessionData");
            go.AddComponent<SessionData>();
        }

        // 2. Logic Unlock Level
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            bool isUnlocked = (i + 1) <= unlockedLevel;

            levelButtons[i].interactable = isUnlocked;
            levelButtons[i].onClick.AddListener(() => LoadLevel(idx));

            var labelText = levelButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            //if (labelText != null)
            //    labelText.text = isUnlocked
            //        ? $"Level {i + 1}"
            //        : $"Level {i + 1} 🔒";
        }

        backButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));
    }

    void LoadLevel(int index)
    {
        if (index >= levelSceneNames.Length) return;

        // Reset dữ liệu coin/kill cho màn chơi mới
        SessionData.Instance?.Reset();

        // ── CHIẾN THUẬT BOOTSTRAP ──────────────────────────────────────────

        if (NetworkManager.Singleton != null)
        {
            // BẮT ĐẦU HOST: Tự biến mình thành server để hệ thống Netcode hoạt động
            // Điều này giúp Player được Spawn tự động và IsOwner được gán đúng
            NetworkManager.Singleton.StartHost();

            // SỬ DỤNG SceneManager của Netcode để load màn chơi
            // LoadSceneMode.Single sẽ xóa sạch scene LevelSelect và thay bằng Level mới
            NetworkManager.Singleton.SceneManager.LoadScene(levelSceneNames[index], LoadSceneMode.Single);

            Debug.Log($"[LevelSelect] Đang khởi tạo Host cho: {levelSceneNames[index]}");
        }
        else
        {
            // Trường hợp lỗi: Nếu không thấy NetworkManager (thường do không chạy từ scene Bootstrap)
            Debug.LogError("KHÔNG TÌM THẤY NETWORKMANAGER! Bạn phải nhấn Play từ scene Bootstrap.");

            // Fallback: Chạy kiểu offline truyền thống (có thể gây lỗi camera/spawn nếu prefab có network)
            SceneManager.LoadScene(levelSceneNames[index]);
        }
    }
}