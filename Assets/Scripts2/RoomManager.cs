using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Room Scene.
///
/// Về ConnectionApproval:
/// - NetworkConfig.ConnectionApproval = true phải được tick trong Unity Inspector
/// - Ở runtime chỉ gán/xóa ConnectionApprovalCallback
/// - Khi callback = null → Netcode tự approve tất cả (nếu flag = true trong Inspector)
///   hoặc không dùng approval (nếu flag = false)
/// </summary>
public class RoomManager : NetworkBehaviour
{
    [Header("Player Slots UI")]
    public PlayerSlotUI[] playerSlots;

    [Header("Map Selection (Host only)")]
    public GameObject mapSelectGroup;
    public Button[] mapButtons;
    public string[] mapSceneNames = { "Level1", "Level2", "Level3", "Level4" };
    public string[] mapDisplayNames = { "Forest", "Desert", "Snow", "Volcano" };

    [Header("Room Info")]
    public TextMeshProUGUI roomTypeText;
    public TextMeshProUGUI selectedMapText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI statusText;

    [Header("Buttons")]
    public Button startButton;
    public Button leaveButton;

    [Header("Scenes")]
    public string lobbyScene = "Lobby";

    // ── NetworkVariables ──────────────────────────────────────────────────
    private NetworkVariable<int> syncedMapIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> syncedPlayerCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkList<ulong> connectedClientIds;
    private NetworkList<Unity.Collections.FixedString64Bytes> playerNames;

    private bool gameStarted = false;

    void Awake()
    {
        connectedClientIds = new NetworkList<ulong>(
            new List<ulong>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        playerNames = new NetworkList<Unity.Collections.FixedString64Bytes>(
            new List<Unity.Collections.FixedString64Bytes>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        syncedMapIndex.OnValueChanged += OnMapChanged;
        syncedPlayerCount.OnValueChanged += OnPlayerCountChanged;
        connectedClientIds.OnListChanged += OnClientListChanged;

        if (IsHost)
        {
            mapSelectGroup?.SetActive(true);
            startButton?.gameObject.SetActive(true);

            // Thay thế callback tạm từ CreateRoomManager bằng callback thật
            // có logic kiểm soát full/gameStarted
            NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;

            int initialMap = 0;
            if (RoomContext.CurrentRoom != null)
                initialMap = Mathf.Clamp(RoomContext.CurrentRoom.mapIndex - 1, 0, 3);
            syncedMapIndex.Value = initialMap;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (!connectedClientIds.Contains(NetworkManager.Singleton.LocalClientId))
                connectedClientIds.Add(NetworkManager.Singleton.LocalClientId);

            string hostName = (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
                ? AuthManager.Username : "Host";
            EnsureNameSlot(0);
            playerNames[0] = hostName;

            syncedPlayerCount.Value = connectedClientIds.Count;
            UpdateBroadcastInfo();
        }
        else
        {
            mapSelectGroup?.SetActive(false);
            startButton?.gameObject.SetActive(false);

            string myName = (AuthManager.Instance?.IsLoggedIn == true)
                ? AuthManager.Username
                : $"Player {NetworkManager.Singleton.LocalClientId}";
            SubmitNameServerRpc(myName, NetworkManager.Singleton.LocalClientId);
        }

        for (int i = 0; i < mapButtons.Length; i++)
        {
            int idx = i;
            mapButtons[i].onClick.RemoveAllListeners();
            mapButtons[i].onClick.AddListener(() => OnMapButtonClicked(idx));
            mapButtons[i].interactable = IsHost;
        }

        startButton?.onClick.RemoveAllListeners();
        startButton?.onClick.AddListener(OnStartClicked);

        leaveButton?.onClick.RemoveAllListeners();
        leaveButton?.onClick.AddListener(OnLeaveClicked);

        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        syncedMapIndex.OnValueChanged -= OnMapChanged;
        syncedPlayerCount.OnValueChanged -= OnPlayerCountChanged;
        connectedClientIds.OnListChanged -= OnClientListChanged;

        if (IsHost && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            // Xóa callback khi rời scene
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }
    }

    // ── Client connect/disconnect (server only) ───────────────────────────

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (!connectedClientIds.Contains(clientId))
            connectedClientIds.Add(clientId);
        syncedPlayerCount.Value = connectedClientIds.Count;
        UpdateBroadcastInfo();
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        int idx = IndexOf(clientId);
        if (idx >= 0)
        {
            connectedClientIds.Remove(clientId);
            if (idx < playerNames.Count)
                playerNames.RemoveAt(idx);
        }
        syncedPlayerCount.Value = connectedClientIds.Count;
        UpdateBroadcastInfo();
    }

    // ── Map selection ─────────────────────────────────────────────────────

    void OnMapButtonClicked(int index)
    {
        if (!IsHost) return;
        ChangeMapServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeMapServerRpc(int mapIndex)
    {
        syncedMapIndex.Value = mapIndex;
        if (RoomContext.CurrentRoom != null)
        {
            RoomContext.CurrentRoom.selectedMap = mapSceneNames[mapIndex];
            RoomContext.CurrentRoom.mapIndex = mapIndex + 1;
        }
        UpdateBroadcastInfo();
    }

    void OnMapChanged(int o, int n) => RefreshUI();
    void OnPlayerCountChanged(int o, int n) => RefreshUI();
    void OnClientListChanged(NetworkListEvent<ulong> e) => RefreshUI();

    // ── Start Game ────────────────────────────────────────────────────────

    void OnStartClicked()
    {
        if (!IsHost) return;
        if (syncedPlayerCount.Value < 2)
        {
            statusText.text = "Cần ít nhất 2 người chơi!";
            return;
        }

        string targetScene = mapSceneNames[syncedMapIndex.Value];
        if (RoomContext.CurrentRoom != null)
            RoomContext.CurrentRoom.selectedMap = targetScene;

        SessionData.Instance?.Reset();
        gameStarted = true;
        LanDiscovery.Instance?.StopBroadcast();

        NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    // ── Leave ─────────────────────────────────────────────────────────────

    void OnLeaveClicked()
    {
        leaveButton.interactable = false;
        if (IsHost)
            StartCoroutine(HostLeaveRoutine());
        else
            StartCoroutine(ClientLeaveRoutine());
    }

    IEnumerator HostLeaveRoutine()
    {
        // Gửi RPC cho client biết host rời
        KickAllClientsToLobbyClientRpc();
        LanDiscovery.Instance?.StopBroadcast();

        // Đợi 2 frame để RPC kịp gửi trước khi Shutdown
        yield return null;
        yield return null;

        DoShutdown();
        SceneManager.LoadScene(lobbyScene);
    }

    IEnumerator ClientLeaveRoutine()
    {
        LanDiscovery.Instance?.StopListening();
        yield return null;
        DoShutdown();
        SceneManager.LoadScene(lobbyScene);
    }

    void DoShutdown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        // Xóa callback sau shutdown — không được reset NetworkConfig flag
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        RoomContext.Clear();
    }

    [ClientRpc]
    private void KickAllClientsToLobbyClientRpc()
    {
        if (IsHost) return;
        Debug.Log("[RoomManager] Host rời → về lobby");
        LanDiscovery.Instance?.StopListening();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        RoomContext.Clear();
        SceneManager.LoadScene(lobbyScene);
    }

    // ── UI ────────────────────────────────────────────────────────────────

    void RefreshUI()
    {
        int mapIdx = syncedMapIndex.Value;
        int playerCount = syncedPlayerCount.Value;

        if (selectedMapText != null && mapIdx < mapDisplayNames.Length)
            selectedMapText.text = "Map: " + mapDisplayNames[mapIdx];

        if (roomTypeText != null && RoomContext.CurrentRoom != null)
            roomTypeText.text = "Room Type: " +
                (RoomContext.CurrentRoom.roomType == "lan" ? "LAN" : "Localhost");

        if (playerCountText != null)
            playerCountText.text = $"Players: {playerCount}/4";

        for (int i = 0; i < mapButtons.Length; i++)
        {
            var colors = mapButtons[i].colors;
            colors.normalColor = (i == mapIdx) ? Color.yellow : Color.white;
            mapButtons[i].colors = colors;
        }

        RefreshSlots();

        if (startButton != null)
            startButton.interactable = IsHost && playerCount >= 2;

        if (statusText != null)
            statusText.text = IsHost
                ? (playerCount < 2 ? "Waiting for players..." : "Ready!")
                : "Waiting for host to start...";
    }

    void RefreshSlots()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < connectedClientIds.Count)
            {
                ulong cid = connectedClientIds[i];
                bool isHost = cid == NetworkManager.ServerClientId;
                string name = (i < playerNames.Count)
                    ? playerNames[i].ToString()
                    : $"Player {i + 1}";
                playerSlots[i].SetOccupied(name, isHost);
            }
            else
            {
                playerSlots[i].SetEmpty();
            }
        }
    }

    void UpdateBroadcastInfo()
    {
        if (!IsHost || RoomContext.CurrentRoom == null) return;
        RoomContext.CurrentRoom.currentPlayers = connectedClientIds.Count;
        LanDiscovery.Instance?.UpdateBroadcastRoom(RoomContext.CurrentRoom);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitNameServerRpc(string name, ulong clientId)
    {
        int idx = IndexOf(clientId);
        if (idx < 0) return;
        EnsureNameSlot(idx);
        playerNames[idx] = name;
    }

    // ── Connection Approval ───────────────────────────────────────────────

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (gameStarted)
        {
            response.Approved = false;
            response.Reason = "Game đã bắt đầu.";
            return;
        }
        if (connectedClientIds.Count >= 4)
        {
            response.Approved = false;
            response.Reason = "Phòng đã đầy.";
            return;
        }
        response.Approved = true;
        response.CreatePlayerObject = false;
        Debug.Log($"[Room] Approved {request.ClientNetworkId}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    int IndexOf(ulong clientId)
    {
        for (int i = 0; i < connectedClientIds.Count; i++)
            if (connectedClientIds[i] == clientId) return i;
        return -1;
    }

    void EnsureNameSlot(int index)
    {
        while (playerNames.Count <= index)
            playerNames.Add("Player");
    }
}