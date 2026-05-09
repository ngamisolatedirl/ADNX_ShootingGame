using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Room Scene: nơi host và client chờ nhau trước khi vào game.
/// - Host thấy nút Start Game và dropdown đổi map.
/// - Client chỉ thấy thông tin phòng.
/// - Khi host Start → tất cả load Level cùng lúc qua NetworkManager.SceneManager.
/// Attach vào RoomScene.
/// </summary>
public class RoomManager : NetworkBehaviour
{
    [Header("Player Slots UI")]
    public PlayerSlotUI[] playerSlots;      // 4 slot UI

    [Header("Map Selection (Host only)")]
    public GameObject mapSelectGroup;       // ẩn với client
    public Button[] mapButtons;             // 4 nút chọn map
    public string[] mapSceneNames = { "Level1", "Level2", "Level3", "Level4" };
    public string[] mapDisplayNames = { "Forest", "Desert", "Snow", "Volcano" };

    [Header("Room Info")]
    public TextMeshProUGUI roomTypeText;
    public TextMeshProUGUI selectedMapText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI statusText;

    [Header("Buttons")]
    public Button startButton;              // chỉ host thấy
    public Button leaveButton;

    [Header("Scenes")]
    public string lobbyScene = "Lobby";

    // ── NetworkVariables (sync host → tất cả client) ──────────────────────
    private NetworkVariable<int> syncedMapIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> syncedPlayerCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Danh sách client info (tên, slot)
    private NetworkList<ulong> connectedClientIds;

    void Awake()
    {
        connectedClientIds = new NetworkList<ulong>(
            new List<ulong>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe sync events
        syncedMapIndex.OnValueChanged += OnMapChanged;
        syncedPlayerCount.OnValueChanged += OnPlayerCountChanged;
        connectedClientIds.OnListChanged += OnClientListChanged;

        // Host setup
        if (IsHost)
        {
            mapSelectGroup?.SetActive(true);
            startButton?.gameObject.SetActive(true);

            // Set map từ RoomContext (chọn lúc tạo phòng)
            int initialMap = 0;
            if (RoomContext.CurrentRoom != null)
                initialMap = Mathf.Clamp(RoomContext.CurrentRoom.mapIndex - 1, 0, 3);
            syncedMapIndex.Value = initialMap;

            // Đăng ký callback client connect/disconnect
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Host tự thêm vào list
            connectedClientIds.Add(NetworkManager.Singleton.LocalClientId);
            syncedPlayerCount.Value = connectedClientIds.Count;

            UpdateBroadcastInfo();
        }
        else
        {
            mapSelectGroup?.SetActive(false);
            startButton?.gameObject.SetActive(false);
        }

        // Setup map buttons
        for (int i = 0; i < mapButtons.Length; i++)
        {
            int idx = i;
            mapButtons[i].onClick.AddListener(() => OnMapButtonClicked(idx));
            mapButtons[i].interactable = IsHost;
        }

        startButton?.onClick.AddListener(OnStartClicked);
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
        }
    }

    // ── Host: quản lý client vào/ra ────────────────────────────────────────

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
        if (connectedClientIds.Contains(clientId))
            connectedClientIds.Remove(clientId);
        syncedPlayerCount.Value = connectedClientIds.Count;
        UpdateBroadcastInfo();
    }

    // ── Map selection ──────────────────────────────────────────────────────

    void OnMapButtonClicked(int index)
    {
        if (!IsHost) return;
        ChangeMapServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeMapServerRpc(int mapIndex)
    {
        syncedMapIndex.Value = mapIndex;

        // Cập nhật RoomContext và broadcast LAN
        if (RoomContext.CurrentRoom != null)
        {
            RoomContext.CurrentRoom.selectedMap = mapSceneNames[mapIndex];
            RoomContext.CurrentRoom.mapIndex = mapIndex + 1;
        }
        UpdateBroadcastInfo();
    }

    void OnMapChanged(int oldVal, int newVal)
    {
        RefreshUI();
    }

    void OnPlayerCountChanged(int oldVal, int newVal)
    {
        RefreshUI();
    }

    void OnClientListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RefreshUI();
    }

    // ── Start Game ─────────────────────────────────────────────────────────

    void OnStartClicked()
    {
        if (!IsHost) return;
        if (syncedPlayerCount.Value < 2)
        {
            statusText.text = "need atleast 2 players!";
            return;
        }

        string targetScene = mapSceneNames[syncedMapIndex.Value];

        // Lưu map đã chọn vào RoomContext để GameManager biết
        if (RoomContext.CurrentRoom != null)
            RoomContext.CurrentRoom.selectedMap = targetScene;

        // Reset session data
        SessionData.Instance?.Reset();

        // Dừng broadcast vì không cần tìm phòng nữa
        LanDiscovery.Instance?.StopBroadcast();

        // Load scene đồng bộ cho tất cả client
        NetworkManager.Singleton.SceneManager.LoadScene(
            targetScene, LoadSceneMode.Single);
    }

    // ── Leave ──────────────────────────────────────────────────────────────

    void OnLeaveClicked()
    {
        if (IsHost)
        {
            LanDiscovery.Instance?.StopBroadcast();
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
        }

        RoomContext.Clear();
        SceneManager.LoadScene(lobbyScene);
    }

    // ── UI Refresh ─────────────────────────────────────────────────────────

    void RefreshUI()
    {
        int mapIdx = syncedMapIndex.Value;
        int playerCount = syncedPlayerCount.Value;

        // Map info
        if (selectedMapText != null && mapIdx < mapDisplayNames.Length)
            selectedMapText.text = "Map: " + mapDisplayNames[mapIdx];

        // Room type
        if (roomTypeText != null && RoomContext.CurrentRoom != null)
            roomTypeText.text = "Room Type: " +
                (RoomContext.CurrentRoom.roomType == "lan" ? "LAN" : "Localhost");

        // Player count
        if (playerCountText != null)
            playerCountText.text = $"Player : {playerCount}/4";

        // Highlight map button được chọn
        for (int i = 0; i < mapButtons.Length; i++)
        {
            var colors = mapButtons[i].colors;
            colors.normalColor = (i == mapIdx) ? Color.yellow : Color.white;
            mapButtons[i].colors = colors;
        }

        // Player slots
        RefreshSlots();

        // Start button: chỉ active khi đủ ≥2 người
        if (startButton != null)
        {
            startButton.interactable = playerCount >= 2;
            statusText.text = playerCount < 2
                ? "Waiting for other players..."
                : "Ready!";
        }
    }

    void RefreshSlots()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < connectedClientIds.Count)
            {
                ulong clientId = connectedClientIds[i];
                bool isMe = clientId == NetworkManager.Singleton.LocalClientId;
                playerSlots[i].SetOccupied(
                    isMe ? "You" : $"Player {i + 1}",
                    clientId == NetworkManager.ServerClientId
                );
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
}