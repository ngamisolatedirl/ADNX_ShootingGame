using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

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

    // ── Internal tracking ─────────────────────────────────────────────────
    private HashSet<ulong> deadPlayers = new HashSet<ulong>();
    private HashSet<ulong> winZonePlayers = new HashSet<ulong>();
    private HashSet<ulong> allPlayers = new HashSet<ulong>();
    private bool isGameOver = false;
    private bool isWon = false;

    // Guard tránh ClientLeaveToMenu chạy 2 lần
    private bool _leaving = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            allPlayers.Clear();
            deadPlayers.Clear();
            winZonePlayers.Clear();
            isGameOver = false;
            isWon = false;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                allPlayers.Add(client.ClientId);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDropped;

            // Re-attach sau scene load — Netcode có thể reset callback trong quá trình scene sync
            if (RoomLock.IsLocked)
                RoomLock.Lock();

            Debug.Log($"[GameManager] OnNetworkSpawn — {allPlayers.Count} players");
            Debug.Log($"[LOG-D] GameManager spawned. IsLocked={RoomLock.IsLocked}, " +
                      $"callback={NetworkManager.Singleton.ConnectionApprovalCallback?.Method.Name ?? "null"}");
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

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (!allPlayers.Contains(clientId))
        {
            Debug.LogWarning($"[GameManager] Kick client lạ {clientId} — game đã bắt đầu");

            // Báo client về menu TRƯỚC khi kick
            NotifyLateJoinClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });

            // Đợi RPC gửi xong rồi mới kick
            StartCoroutine(DelayedKick(clientId));
            return;
        }

        Debug.Log($"[GameManager] Client {clientId} connected, total: {allPlayers.Count}");
    }

    [ClientRpc]
    void NotifyLateJoinClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsHost) return;
        Debug.Log("[GameManager] Bị từ chối — game đã bắt đầu, về menu");

        // Hiện thông báo nếu có UI, hoặc về thẳng menu
        StartCoroutine(ShowMessageAndLeave());
    }

    IEnumerator ShowMessageAndLeave()
    {
        // Nếu có UI thông báo thì bật lên đây
        // statusUI?.SetActive(true);
        // statusText.text = "Game đã bắt đầu!";

        yield return new WaitForSecondsRealtime(2f); // đợi 2 giây cho người chơi đọc

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        RoomContext.Clear();
        SceneManager.LoadScene(mainMenuScene);
    }

    IEnumerator DelayedKick(ulong clientId)
    {
        yield return new WaitForSecondsRealtime(0.2f); // đợi RPC gửi đi
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.DisconnectClient(clientId, "Game đã bắt đầu.");
    }

    void OnClientDropped(ulong clientId)
    {
        if (!IsServer) return;

        allPlayers.Remove(clientId);
        deadPlayers.Discard(clientId);
        winZonePlayers.Discard(clientId);

        Debug.Log($"[GameManager] Client {clientId} dropped. Còn {allPlayers.Count} người.");

        if (isGameOver) { GameOverClientRpc(); return; }
        if (isWon) { WinClientRpc(); return; }

        if (allPlayers.Count == 0) { TriggerGameOver(); return; }
        if (deadPlayers.Count >= allPlayers.Count) TriggerGameOver();
        else if (allPlayers.Count > 0 && winZonePlayers.Count >= AlivePlayerCount) TriggerWin();
    }

    // ── Player Death ───────────────────────────────────────────────────────

    public void ReportPlayerDeath(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return;
        if (isGameOver || isWon) return;

        deadPlayers.Add(clientId);
        winZonePlayers.Discard(clientId);
        Debug.Log($"[GameManager] Player {clientId} chết. Dead: {deadPlayers.Count}/{allPlayers.Count}");

        if (NetworkUtils.IsOnline)
            NotifyPlayerDeadClientRpc(clientId);

        if (deadPlayers.Count >= allPlayers.Count)
            TriggerGameOver();
    }

    public void GameOver()
    {
        if (!NetworkUtils.IsOnline) TriggerGameOver();
        else ReportPlayerDeath(0);
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("[GameManager] GAME OVER");
        if (NetworkUtils.IsOnline) GameOverClientRpc();
        else ShowGameOverUI();
    }

    // ── Win Zone ───────────────────────────────────────────────────────────

    public bool ReportPlayerInWinZone(ulong clientId)
    {
        if (!NetworkUtils.HasServerAuthority) return false;
        if (isGameOver || isWon) return false;
        if (deadPlayers.Contains(clientId)) return false;

        winZonePlayers.Add(clientId);
        int inZone = winZonePlayers.Count;
        int needed = AlivePlayerCount;

        Debug.Log($"[GameManager] WinZone: {inZone}/{needed}");

        if (NetworkUtils.IsOnline)
            NotifyWaitingClientRpc(inZone, needed);

        if (inZone >= needed) { TriggerWin(); return true; }
        return false;
    }

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
        if (!NetworkUtils.IsOnline) TriggerWin();
        else ReportPlayerInWinZone(0);
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

        if (NetworkUtils.IsOnline) WinClientRpc();
        else ShowWinUI();
    }

    // ── Client chủ động thoát về menu ─────────────────────────────────────

    public void ClientLeaveToMenu()
    {
        if (_leaving) return;

        if (IsHost)
            StartCoroutine(HostLeaveToMenuRoutine());
        else
            StartCoroutine(ClientLeaveToMenuRoutine());
    }

    private IEnumerator HostLeaveToMenuRoutine()
    {
        _leaving = true;
        Time.timeScale = 1f;

        ForceClientsToMenuClientRpc();

        yield return null;
        yield return null;

        DoLocalLeave();
    }

    private IEnumerator ClientLeaveToMenuRoutine()
    {
        _leaving = true;
        Time.timeScale = 1f;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            RequestLeaveServerRpc(NetworkManager.Singleton.LocalClientId);

        yield return new WaitForSecondsRealtime(0.1f);

        DoLocalLeave();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLeaveServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"[GameManager] Client {clientId} yêu cầu thoát về menu.");

        allPlayers.Remove(clientId);
        deadPlayers.Discard(clientId);
        winZonePlayers.Discard(clientId);

        if (clientId != NetworkManager.ServerClientId)
            NetworkManager.Singleton.DisconnectClient(clientId, "Người chơi tự thoát về menu.");

        if (!isGameOver && !isWon)
        {
            if (allPlayers.Count == 0)
                TriggerGameOver();
            else if (deadPlayers.Count >= allPlayers.Count)
                TriggerGameOver();
        }
    }

    [ClientRpc]
    private void ForceClientsToMenuClientRpc()
    {
        if (IsHost) return;
        if (_leaving) return;
        _leaving = true;
        Debug.Log("[GameManager] Host thoát → client về main menu.");
        DoLocalLeave();
    }

    private void DoLocalLeave()
    {
        LanDiscovery.Instance?.StopBroadcast();
        LanDiscovery.Instance?.StopListening();
        SessionData.Instance?.Reset();
        RoomContext.Clear();

        RoomLock.Unlock();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuScene);
    }

    // ── ClientRpc ──────────────────────────────────────────────────────────

    [ClientRpc]
    void GameOverClientRpc()
    {
        ShowGameOverUI();
        if (restartButton != null) restartButton.SetActive(NetworkUtils.IsHost);
        if (waitingHostRestartText != null) waitingHostRestartText.SetActive(!NetworkUtils.IsHost);
    }

    [ClientRpc]
    void WinClientRpc()
    {
        ShowWinUI();
        if (restartButton != null) restartButton.SetActive(NetworkUtils.IsHost);
        if (waitingHostRestartText != null) waitingHostRestartText.SetActive(!NetworkUtils.IsHost);
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

    // ── Restart / Return (Host only) ───────────────────────────────────────

    public void RestartGame()
    {
        if (NetworkUtils.IsOnline && !NetworkUtils.IsHost) return;
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        Time.timeScale = 1f;

        if (NetworkUtils.IsOnline)
        {
            KickGhostClients();
            yield return null;
            yield return null;
        }

        SessionData.Instance?.Reset();

        var healthBar = FindObjectOfType<HealthBarUI>();
        healthBar?.ResetToFull();

        if (NetworkUtils.IsOnline)
            ResetTimeScaleClientRpc();

        NetworkManager.Singleton.SceneManager.LoadScene(
            SceneManager.GetActiveScene().name,
            LoadSceneMode.Single);
    }

    private void KickGhostClients()
    {
        if (!IsServer) return;
        var toKick = new List<ulong>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong cid = client.ClientId;
            if (cid == NetworkManager.ServerClientId) continue;
            if (!allPlayers.Contains(cid))
                toKick.Add(cid);
        }

        foreach (var cid in toKick)
        {
            Debug.Log($"[GameManager] Kick ghost client {cid} trước khi restart");
            NetworkManager.Singleton.DisconnectClient(cid, "Bạn đã thoát khỏi ván game này.");
        }
    }

    public void ReturnToLobby()
    {
        if (NetworkUtils.IsOnline && !NetworkUtils.IsHost) return;

        Time.timeScale = 1f;
        SessionData.Instance?.FlushAndReset();

        if (NetworkUtils.IsOnline)
        {
            // Unlock để Room scene có thể gán lại ApproveConnection
            RoomLock.Unlock();
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