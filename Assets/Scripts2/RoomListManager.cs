using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình danh sách phòng (Join).
/// Client tìm phòng LAN qua UDP broadcast.
/// Attach vào RoomListScene.
/// </summary>
public class RoomListManager : MonoBehaviour
{
    [Header("UI")]
    public Transform roomListContainer;     // ScrollView Content
    public GameObject roomItemPrefab;       // prefab mỗi dòng phòng
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
    public string roomScene  = "Room";

    private List<RoomInfo> currentRooms = new List<RoomInfo>();
    private bool manualMode = false;

    void Start()
    {
        refreshButton.onClick.AddListener(Refresh);
        backButton.onClick.AddListener(OnBack);
        manualJoinButton.onClick.AddListener(OnManualJoin);
        toggleManualButton.onClick.AddListener(ToggleManualMode);

        manualIPInput.text = "127.0.0.1";
        manualJoinGroup.SetActive(false);

        // Subscribe LAN discovery
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
        // Xóa các item cũ
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
        if (room.IsFull)
        {
            statusText.text = "Phòng đã đầy!";
            return;
        }

        ConnectToRoom(room.hostIP, room.port, room);
    }

    void OnManualJoin()
    {
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
        RoomContext.CurrentRoom = room;
        RoomContext.IsHost = false;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        NetworkManager.Singleton.StartClient();

        statusText.text = $"Đang kết nối tới {ip}:{port}...";
    }

    void OnConnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        // RoomScene sẽ được load bởi host qua NetworkManager.SceneManager
        statusText.text = "Đã kết nối! Chờ vào phòng...";
    }

    void OnDisconnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        statusText.text = "Kết nối thất bại. Thử lại?";
    }

    void OnBack()
    {
        LanDiscovery.Instance?.StopListening();
        if (NetworkManager.Singleton.IsClient)
            NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(lobbyScene);
    }
}
