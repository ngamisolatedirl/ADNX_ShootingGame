using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Quản lý trạng thái ván game (win/lose).
/// - Offline: 1 player chết → game over. Vào WinZone → win.
/// - Online:  1 người chết → chuyển cam sang người còn sống.
///            Tất cả chết → game over.
///            Chỉ Host mới thấy nút Restart/Return.
///
/// FIX:
/// 1. ResetTimeScaleClientRpc() — reset Time.timeScale = 1 trên tất cả client khi restart
/// 2. OnClientConnected callback — rebuild allPlayers đúng sau scene reload
/// 3. ReportPlayerLeftWinZone() — player rời zone thì remove khỏi winZonePlayers
/// 4. AlivePlayerCount tách riêng — tránh nhầm với ActivePlayerCount
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public GameObject gameOverUI;
    public GameObject winUI;
    public GameObject waitingForTeammatesUI;

    [Header("Online GameOver UI")]
    [Tooltip("Nút Restart - chỉ bật trên máy Host")]
    public GameObject restartButton;
    [Tooltip("Text hiển thị cho client khi chờ host restart")]
    public GameObject waitingHostRestartText;

    [Header("Scene")]
    public int currentLevelIndex = 1;
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
            // Reset state mỗi lần scene load (quan trọng khi restart)
            allPlayers.Clear();
            deadPlayers.Clear();
            winZonePlayers.Clear();
            isGameOver = false;
            isWon = false;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                allPlayers.Add(client.ClientId);

            // FIX: lắng nghe thêm client kết nối muộn sau scene load
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDropped;

            Debug.Log($"[GameManager] OnNetworkSpawn — {allPlayers.Count} players");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDropped;
        }
    }

    void Start()
    {
        if (!NetworkUtils.IsOnline)
        {
            allPlayers.Clear();
            allPlayers.Add(0);
        }
    }

    // ── Player Connect / Disconnect ────────────────────────────────────────

    // FIX: client kết nối sau khi scene đã load (thường gặp khi restart)
    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        allPlayers.Add(clientId);
        Debug.Log($"[GameManager] Client {clientId} connected, total: {allPlayers.Count}");
    }

    void OnClientDropped(ulong clientId)
    {
        if (!IsServer) return;

        allPlayers.Remove(clientId);
        deadPlayers.Discard(clientId);
        winZonePlayers.Discard(clientId);

        if (allPlayers.Count > 0 && deadPlayers.Count >= allPlayers.Count)
            TriggerGameOver();
        else if (allPlayers.Count > 0 && winZonePlayers.Count >= AlivePlayerCount)
            TriggerWin();
    }

    // ── Player Death ───────────────────────────────────────────────────────

    public void ReportPlayerDeath(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;

        deadPlayers.Add(clientId);
        winZonePlayers.Discard(clientId); // chết thì không tính trong winzone nữa
        Debug.Log($"[GameManager] Player {clientId} chết. Dead: {deadPlayers.Count}/{allPlayers.Count}");

        if (NetworkUtils.IsOnline)
            NotifyPlayerDeadClientRpc(clientId);

        if (deadPlayers.Count >= allPlayers.Count)
            TriggerGameOver();
    }

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

    public void ReportPlayerInWinZone(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;
        if (deadPlayers.Contains(clientId)) return;  // người chết không tính

        winZonePlayers.Add(clientId);

        int inZone = winZonePlayers.Count;
        int needed = AlivePlayerCount;

        Debug.Log($"[GameManager] WinZone: {inZone}/{needed}");

        if (NetworkUtils.IsOnline)
            NotifyWaitingClientRpc(inZone, needed);

        if (inZone >= needed)
            TriggerWin();
    }

    // FIX: player rời WinZone → remove khỏi winZonePlayers, cập nhật UI chờ
    public void ReportPlayerLeftWinZone(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;

        winZonePlayers.Discard(clientId);

        int inZone = winZonePlayers.Count;
        int needed = AlivePlayerCount;

        Debug.Log($"[GameManager] Rời WinZone: {inZone}/{needed}");

        if (NetworkUtils.IsOnline)
            NotifyWaitingClientRpc(inZone, needed);
    }

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

        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        if (currentLevelIndex >= unlockedLevel)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentLevelIndex + 1);
            PlayerPrefs.Save();
        }

        SessionData.Instance?.FlushAndReset();

        if (NetworkUtils.IsOnline)
            WinClientRpc();
        else
            ShowWinUI();
    }

    // ── ClientRpc ──────────────────────────────────────────────────────────

    [ClientRpc]
    void GameOverClientRpc()
    {
        ShowGameOverUI();

        if (restartButton != null)
            restartButton.SetActive(NetworkUtils.IsHost);

        if (waitingHostRestartText != null)
            waitingHostRestartText.SetActive(!NetworkUtils.IsHost);
    }

    [ClientRpc]
    void WinClientRpc()
    {
        ShowWinUI();

        if (restartButton != null)
            restartButton.SetActive(NetworkUtils.IsHost);

        if (waitingHostRestartText != null)
            waitingHostRestartText.SetActive(!NetworkUtils.IsHost);
    }

    [ClientRpc]
    void NotifyPlayerDeadClientRpc(ulong deadClientId)
    {
        CameraFollow localCam = Camera.main?.GetComponent<CameraFollow>();
        if (localCam != null)
            localCam.FindNearestAlivePlayer();
    }

    [ClientRpc]
    void NotifyWaitingClientRpc(int inZone, int total)
    {
        if (waitingForTeammatesUI == null) return;

        bool waiting = inZone < total;
        waitingForTeammatesUI.SetActive(waiting);

        var txt = waitingForTeammatesUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null)
            txt.text = $"Chờ đồng đội... ({inZone}/{total})";
    }

    // FIX: reset Time.timeScale trên tất cả client trước khi load scene
    [ClientRpc]
    void ResetTimeScaleClientRpc()
    {
        Time.timeScale = 1f;
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

    // ── Buttons ────────────────────────────────────────────────────────────

    public void RestartGame()
    {
        if (NetworkUtils.IsOnline && !NetworkUtils.IsHost) return;

        Time.timeScale = 1f;
        SessionData.Instance?.Reset();

        var healthBar = FindObjectOfType<HealthBarUI>();
        healthBar?.ResetToFull();

        // FIX: reset timeScale trên tất cả client TRƯỚC khi load scene
        if (NetworkUtils.IsOnline)
            ResetTimeScaleClientRpc();

        NetworkManager.Singleton.SceneManager.LoadScene(
            SceneManager.GetActiveScene().name,
            LoadSceneMode.Single);
    }

    public void ReturnToLobby()
    {
        if (NetworkUtils.IsOnline && !NetworkUtils.IsHost) return;

        Time.timeScale = 1f;
        SessionData.Instance?.FlushAndReset();

        if (NetworkUtils.IsOnline)
        {
            // FIX: reset timeScale client trước khi chuyển scene
            ResetTimeScaleClientRpc();
            NetworkManager.Singleton.SceneManager.LoadScene(roomScene, LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// Số player còn sống (chưa chết) — dùng cho điều kiện WinZone
    private int AlivePlayerCount =>
        Mathf.Max(1, allPlayers.Count - deadPlayers.Count);

    public bool IsGameOver => isGameOver;
    public bool IsWon => isWon;

    public bool IsPlayerDead(ulong clientId) => deadPlayers.Contains(clientId);
}

public static class HashSetExtensions
{
    public static void Discard<T>(this HashSet<T> set, T item) => set.Remove(item);
}