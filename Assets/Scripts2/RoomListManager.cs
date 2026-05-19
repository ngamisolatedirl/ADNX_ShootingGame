using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình danh sách phòng (Join).
/// Fix: Chỉ xóa ConnectionApprovalCallback (không đụng NetworkConfig flag),
///      Shutdown sạch trước StartClient.
/// </summary>
public class RoomListManager : MonoBehaviour
{
    [Header("UI")]
    public Transform roomListContainer;
    public GameObject roomItemPrefab;
    public Button refreshButton;
    public Button backButton;
    public TextMeshProUGUI statusText;

    [Header("Manual Join (Localhost)")]
    public TMP_InputField manualIPInput;
    public Button manualJoinButton;
    public GameObject manualJoinGroup;
    public Button toggleManualButton;

    [Header("Scenes")]
    public string lobbyScene = "Lobby";
    public string roomScene = "Room";

    private List<RoomInfo> currentRooms = new List<RoomInfo>();
    private bool manualMode = false;
    private bool isConnecting = false;

    void Start()
    {
        // Shutdown nếu còn từ session trước
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[RoomList] Shutdown session cũ");
        }

        // Xóa approval callback — không đụng vào NetworkConfig.ConnectionApproval flag
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        refreshButton.onClick.AddListener(Refresh);
        backButton.onClick.AddListener(OnBack);
        manualJoinButton.onClick.AddListener(OnManualJoin);
        toggleManualButton.onClick.AddListener(ToggleManualMode);

        manualIPInput.text = "127.0.0.1";
        manualJoinGroup.SetActive(false);

        LanDiscovery.Instance.OnRoomListUpdated += OnRoomListUpdated;
        LanDiscovery.Instance.StartListening();
        statusText.text = "Đang tìm phòng...";
    }

    void OnDestroy()
    {
        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.OnRoomListUpdated -= OnRoomListUpdated;
            LanDiscovery.Instance.StopListening();
        }
    }

    void Refresh()
    {
        LanDiscovery.Instance.FireRoomListUpdate();
        statusText.text = "Đang làm mới...";
    }

    void ToggleManualMode()
    {
        manualMode = !manualMode;
        manualJoinGroup.SetActive(manualMode);
        toggleManualButton.GetComponentInChildren<TextMeshProUGUI>().text
            = manualMode ? "Ẩn nhập thủ công" : "Nhập IP thủ công";
    }

    void OnRoomListUpdated(List<RoomInfo> rooms)
    {
        currentRooms = rooms;
        RefreshRoomListUI();
    }

    void RefreshRoomListUI()
    {
        foreach (Transform child in roomListContainer)
            Destroy(child.gameObject);

        if (currentRooms.Count == 0)
        {
            statusText.text = "Không tìm thấy phòng nào.";
            return;
        }

        statusText.text = $"Tìm thấy {currentRooms.Count} phòng:";
        foreach (var room in currentRooms)
        {
            var item = Instantiate(roomItemPrefab, roomListContainer);
            var roomItem = item.GetComponent<RoomListItem>();
            if (roomItem != null)
                roomItem.Setup(room, () => JoinRoom(room));
        }
    }

    void JoinRoom(RoomInfo room)
    {
        if (isConnecting) return;
        if (room.IsFull) { statusText.text = "Phòng đã đầy!"; return; }
        ConnectToRoom(room.hostIP, room.port, room);
    }

    void OnManualJoin()
    {
        if (isConnecting) return;
        string ip = manualIPInput.text;
        if (string.IsNullOrEmpty(ip)) return;

        var room = new RoomInfo
        {
            hostIP = ip,
            port = 7777,
            roomType = "localhost",
            selectedMap = "Level1",
            mapIndex = 1
        };
        ConnectToRoom(ip, 7777, room);
    }

    void ConnectToRoom(string ip, ushort port, RoomInfo room)
    {
        isConnecting = true;

        // Shutdown nếu còn sót (ví dụ lần join trước fail giữa chừng)
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            // Không dùng WaitUntil ở đây — Shutdown() của Netcode là gần-đồng-bộ
            // khi gọi từ ngoài game loop, IsListening về false ngay frame tiếp
        }

        // Xóa approval callback (không set flag)
        NetworkManager.Singleton.ConnectionApprovalCallback = null;

        RoomContext.CurrentRoom = room;
        RoomContext.IsHost = false;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;

        string playerName = (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
            ? AuthManager.Username
            : $"Guest_{SystemInfo.deviceUniqueIdentifier.Substring(0, 6)}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.Text.Encoding.UTF8.GetBytes(playerName);

        bool ok = NetworkManager.Singleton.StartClient();
        if (!ok)
        {
            statusText.text = "StartClient thất bại. Thử lại.";
            isConnecting = false;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
            return;
        }

        statusText.text = $"Đang kết nối tới {ip}:{port}...";
    }

    void OnConnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        isConnecting = false;
        statusText.text = "Đã kết nối! Chờ vào phòng...";
    }

    void OnDisconnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        isConnecting = false;
        NetworkManager.Singleton.ConnectionApprovalCallback = null;

        string reason = NetworkManager.Singleton.DisconnectReason;
        statusText.text = string.IsNullOrEmpty(reason)
            ? "Kết nối thất bại. Thử lại?"
            : reason;
    }

    void OnBack()
    {
        LanDiscovery.Instance?.StopListening();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        SceneManager.LoadScene(lobbyScene);
    }
}