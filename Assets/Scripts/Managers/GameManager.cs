using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Quản lý trạng thái ván game (win/lose).
/// - Offline: 1 player chết → game over. Vào WinZone → win.
/// - Online:  tất cả chết → game over. Tất cả vào WinZone → win.
///   Chỉ server mới ra quyết định, client nhận lệnh qua ClientRpc.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public GameObject gameOverUI;
    public GameObject winUI;
    public GameObject waitingForTeammatesUI;   // hiện khi chờ đồng đội vào WinZone

    [Header("Scene")]
    public int currentLevelIndex = 1;          // 1-4, set trong Inspector mỗi Level scene
    public string roomScene = "Room";
    public string mainMenuScene = "MainMenu";

    // ── Internal tracking ──────────────────────────────────────────────────
    private HashSet<ulong> deadPlayers = new HashSet<ulong>();
    private HashSet<ulong> winZonePlayers = new HashSet<ulong>();
    private HashSet<ulong> allPlayers = new HashSet<ulong>();
    private bool isGameOver = false;
    private bool isWon = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Thu thập danh sách tất cả client
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                allPlayers.Add(client.ClientId);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDropped;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDropped;
    }

    // Dùng cho offline (không có NetworkManager)
    void Start()
    {
        if (!NetworkUtils.IsOnline)
        {
            // Offline: chỉ 1 player, tự quản lý
            allPlayers.Clear();
            allPlayers.Add(0); // fake clientId = 0
        }
    }

    // ── Player Death ───────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ PlayerHealth khi player chết.
    /// Online: gọi từ server. Offline: gọi trực tiếp.
    /// </summary>
    public void ReportPlayerDeath(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;

        deadPlayers.Add(clientId);
        Debug.Log($"[GameManager] Player {clientId} chết. Dead: {deadPlayers.Count}/{allPlayers.Count}");

        // Thông báo cho tất cả: player này chết (để camera chuyển)
        if (NetworkUtils.IsOnline)
            NotifyPlayerDeadClientRpc(clientId);

        // Kiểm tra thua: tất cả đều chết
        if (deadPlayers.Count >= allPlayers.Count)
            TriggerGameOver();
    }

    /// <summary>Offline shortcut</summary>
    public void GameOver()
    {
        if (!NetworkUtils.IsOnline)
            TriggerGameOver();
        else
            ReportPlayerDeath(0);
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] GAME OVER");

        if (NetworkUtils.IsOnline)
            GameOverClientRpc();
        else
            ShowGameOverUI();
    }

    // ── Win Zone ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ WinZone khi player bước vào.
    /// </summary>
    public void ReportPlayerInWinZone(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;

        // Không tính player đã chết
        if (deadPlayers.Contains(clientId)) return;

        winZonePlayers.Add(clientId);
        Debug.Log($"[GameManager] Player {clientId} vào WinZone. {winZonePlayers.Count}/{ActivePlayerCount}");

        // Thông báo "đang chờ đồng đội"
        if (NetworkUtils.IsOnline)
            NotifyWaitingClientRpc((int)winZonePlayers.Count, ActivePlayerCount);

        // Win khi tất cả player còn sống đều vào WinZone
        if (winZonePlayers.Count >= ActivePlayerCount)
            TriggerWin();
    }

    /// <summary>Offline shortcut</summary>
    public void WinLevel()
    {
        if (!NetworkUtils.IsOnline)
            TriggerWin();
        else
            ReportPlayerInWinZone(0);
    }

    private void TriggerWin()
    {
        if (isWon) return;
        isWon = true;

        Debug.Log("[GameManager] WIN!");

        // Unlock level tiếp theo
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        if (currentLevelIndex >= unlockedLevel)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentLevelIndex + 1);
            PlayerPrefs.Save();
        }

        // Flush session coins
        SessionData.Instance?.FlushAndReset();

        if (NetworkUtils.IsOnline)
            WinClientRpc();
        else
            ShowWinUI();
    }

    // ── Client Disconnect giữa chừng ───────────────────────────────────────

    void OnClientDropped(ulong clientId)
    {
        if (!IsServer) return;
        // Coi như player đó đã chết
        allPlayers.Remove(clientId);
        deadPlayers.Discard(clientId);
        winZonePlayers.Discard(clientId);

        // Kiểm tra lại điều kiện win/lose
        if (allPlayers.Count > 0 && deadPlayers.Count >= allPlayers.Count)
            TriggerGameOver();
        else if (allPlayers.Count > 0 && winZonePlayers.Count >= ActivePlayerCount)
            TriggerWin();
    }

    // ── ClientRpc ─────────────────────────────────────────────────────────

    [ClientRpc]
    void GameOverClientRpc() => ShowGameOverUI();

    [ClientRpc]
    void WinClientRpc() => ShowWinUI();

    [ClientRpc]
    void NotifyPlayerDeadClientRpc(ulong clientId)
    {
        // CameraFollow sẽ tự tìm target mới khi nhận event này
        CameraFollow.Instance?.OnPlayerDied(clientId);
    }

    [ClientRpc]
    void NotifyWaitingClientRpc(int inZone, int total)
    {
        if (waitingForTeammatesUI != null)
        {
            waitingForTeammatesUI.SetActive(inZone < total);
            var txt = waitingForTeammatesUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null)
                txt.text = $"Chờ đồng đội... ({inZone}/{total})";
        }
    }

    // ── UI helpers ─────────────────────────────────────────────────────────

    void ShowGameOverUI()
    {
        Time.timeScale = 0f;
        gameOverUI?.SetActive(true);
    }

    void ShowWinUI()
    {
        Time.timeScale = 0f;
        winUI?.SetActive(true);
    }

    // ── Buttons (gọi từ UI) ────────────────────────────────────────────────

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (NetworkUtils.IsOnline)
        {
            // Host load lại level hiện tại cho tất cả
            if (NetworkUtils.IsHost)
                NetworkManager.Singleton.SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name,
                    LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        SessionData.Instance?.FlushAndReset();

        if (NetworkUtils.IsOnline)
        {
            if (NetworkUtils.IsHost)
                NetworkManager.Singleton.SceneManager.LoadScene(roomScene, LoadSceneMode.Single);
            // client tự load sau khi nhận scene change từ host
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// Số player còn sống (chưa chết)
    private int ActivePlayerCount =>
        Mathf.Max(1, allPlayers.Count - deadPlayers.Count);

    public bool IsGameOver => isGameOver;
    public bool IsWon => isWon;
}

// Extension để tránh exception khi Remove item không tồn tại
public static class HashSetExtensions
{
    public static void Discard<T>(this HashSet<T> set, T item) => set.Remove(item);
}
