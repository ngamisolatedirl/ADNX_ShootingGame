using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;


public class CreateRoomManager : MonoBehaviour
{
    [Header("Room Type")]
    public Button btnLAN;
    public Button btnLocalhost;
    public TMP_InputField ipInput;
    public GameObject ipInputGroup;

    [Header("Map Selection")]
    public Button[] mapButtons;
    public string[] mapSceneNames = { "Level1", "Level2", "Level3", "Level4" };
    public string[] mapDisplayNames = { "Forest", "Desert", "Snow", "Volcano" };
    public Image mapPreviewImage;
    public Sprite[] mapPreviews;

    [Header("Room Info Display")]
    public TextMeshProUGUI roomTypeText;
    public TextMeshProUGUI selectedMapText;
    public TextMeshProUGUI statusText;

    [Header("Action")]
    public Button createButton;
    public Button backButton;

    [Header("Scenes")]
    public string lobbyScene = "Lobby";
    public string roomScene = "Room";

    private string roomType = "lan";
    private int selectedMapIndex = 0;

    void Start()
    {
        // Shutdown sạch nếu còn sót từ session trước
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[CreateRoom] Shutdown session cũ");
        }
        RoomLock.Unlock();
        // Xóa callback để không còn approval logic từ session host trước
        if (NetworkManager.Singleton != null && !RoomLock.IsLocked)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        ipInput.text = "127.0.0.1";
        ipInputGroup.SetActive(false);

        btnLAN.onClick.AddListener(() => SetRoomType("lan"));
        btnLocalhost.onClick.AddListener(() => SetRoomType("localhost"));

        for (int i = 0; i < mapButtons.Length; i++)
        {
            int idx = i;
            mapButtons[i].onClick.AddListener(() => SelectMap(idx));
        }

        createButton.onClick.AddListener(OnCreateRoom);
        backButton.onClick.AddListener(() => SceneManager.LoadScene(lobbyScene));

        SelectMap(0);
        SetRoomType("lan");
    }

    void SetRoomType(string type)
    {
        roomType = type;
        ipInputGroup.SetActive(type == "localhost");
        roomTypeText.text = "RoomType: " + (type == "lan" ? "LAN" : "Localhost");
    }

    void SelectMap(int index)
    {
        selectedMapIndex = index;
        selectedMapText.text = "Map: " + mapDisplayNames[index];

        if (mapPreviewImage != null && mapPreviews != null && index < mapPreviews.Length)
            mapPreviewImage.sprite = mapPreviews[index];

        for (int i = 0; i < mapButtons.Length; i++)
        {
            var colors = mapButtons[i].colors;
            colors.normalColor = (i == index) ? Color.yellow : Color.white;
            mapButtons[i].colors = colors;
        }
    }

    void OnCreateRoom()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            statusText.text = "Đang chờ shutdown...";
            return; 
        }

        createButton.interactable = false;
        statusText.text = "Đang tạo phòng...";

        string hostIP = roomType == "localhost" ? ipInput.text : GetLocalIP();

        var pendingRoom = new RoomInfo
        {
            roomId = Guid.NewGuid().ToString(),
            hostName = (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
                                 ? AuthManager.Username : "Host",
            roomType = roomType,
            selectedMap = mapSceneNames[selectedMapIndex],
            mapIndex = selectedMapIndex + 1,
            currentPlayers = 1,
            maxPlayers = 4,
            hostIP = hostIP,
            port = 7777
        };

        RoomContext.CurrentRoom = pendingRoom;
        RoomContext.IsHost = true;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(hostIP, 7777);

        // Gán approval callback TRƯỚC StartHost
        // (NetworkConfig.ConnectionApproval phải được tick trong Inspector)
        NetworkManager.Singleton.ConnectionApprovalCallback = ApproveAllConnections;

        bool success = NetworkManager.Singleton.StartHost();

        if (!success)
        {
            Debug.LogError("[CreateRoom] StartHost thất bại!");
            statusText.text = "Tạo phòng thất bại. Thử lại.";
            createButton.interactable = true;
            RoomContext.Clear();
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
            return;
        }

        Debug.Log($"[CreateRoom] Host OK: {hostIP}:7777");

        if (roomType == "lan")
            LanDiscovery.Instance?.StartBroadcast(pendingRoom);

        NetworkManager.Singleton.SceneManager.LoadScene(roomScene, LoadSceneMode.Single);
    }

    // Callback tạm thời: chấp nhận tất cả kết nối
    // RoomManager sẽ thay thế callback này bằng ApproveConnection() sau khi spawn
    void ApproveAllConnections(
        NetworkManager.ConnectionApprovalRequest req,
        NetworkManager.ConnectionApprovalResponse res)
    {
        res.Approved = true;
        res.CreatePlayerObject = false;
        Debug.Log($"[CreateRoom] Pre-approve client {req.ClientNetworkId}");
    }

    string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Debug.Log($"[CreateRoom] Local IP: {ip}");
                    return ip.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CreateRoom] GetLocalIP error: {e.Message}");
        }
        return "127.0.0.1";
    }
}