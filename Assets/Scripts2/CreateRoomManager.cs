using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình tạo phòng (Host).
/// Host chọn loại phòng (LAN / Localhost) và map, rồi Start Host.
/// Attach vào CreateRoomScene.
/// </summary>
public class CreateRoomManager : MonoBehaviour
{
    [Header("Room Type")]
    public Button btnLAN;
    public Button btnLocalhost;
    public TMP_InputField ipInput;          // chỉ hiện khi chọn Localhost
    public GameObject ipInputGroup;         // group chứa label + input IP

    [Header("Map Selection")]
    public Button[] mapButtons;             // 4 nút map (Level1~4)
    public string[] mapSceneNames = { "Level1", "Level2", "Level3", "Level4" };
    public string[] mapDisplayNames = { "Forest", "Desert", "Snow", "Volcano" };
    public Image mapPreviewImage;           // (optional) preview ảnh map
    public Sprite[] mapPreviews;

    [Header("Room Info Display")]
    public TextMeshProUGUI roomTypeText;
    public TextMeshProUGUI selectedMapText;
    public TextMeshProUGUI statusText;

    [Header("Action")]
    public Button createButton;
    public Button backButton;

    [Header("Scenes")]
    public string lobbyScene  = "Lobby";
    public string roomScene   = "Room";

    // ── Internal state ─────────────────────────────────────────────────────
    private string roomType   = "lan";      // "lan" | "localhost"
    private int selectedMapIndex = 0;
    private RoomInfo pendingRoom;

    void Start()
    {
        // Default
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
        roomTypeText.text = "Loại phòng: " + (type == "lan" ? "LAN" : "Localhost");
        RefreshUI();
    }

    void SelectMap(int index)
    {
        selectedMapIndex = index;
        selectedMapText.text = "Map: " + mapDisplayNames[index];

        if (mapPreviewImage != null && mapPreviews != null && index < mapPreviews.Length)
            mapPreviewImage.sprite = mapPreviews[index];

        // Highlight nút được chọn
        for (int i = 0; i < mapButtons.Length; i++)
        {
            var colors = mapButtons[i].colors;
            colors.normalColor = (i == index) ? Color.yellow : Color.white;
            mapButtons[i].colors = colors;
        }
    }

    void RefreshUI()
    {
        // Có thể thêm logic disable/enable tùy state
    }

    void OnCreateRoom()
    {
        string hostIP = roomType == "localhost"
            ? ipInput.text
            : GetLocalIP();

        pendingRoom = new RoomInfo
        {
            roomId       = Guid.NewGuid().ToString(),
            hostName     = "Host",          // có thể lấy từ PlayerPrefs sau
            roomType     = roomType,
            selectedMap  = mapSceneNames[selectedMapIndex],
            mapIndex     = selectedMapIndex + 1,
            currentPlayers = 1,
            maxPlayers   = 4,
            hostIP       = hostIP,
            port         = 7777
        };

        // Lưu room info để RoomScene dùng
        RoomContext.CurrentRoom = pendingRoom;
        RoomContext.IsHost = true;

        // Setup transport
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(hostIP, 7777);

        // Start host
        NetworkManager.Singleton.StartHost();

        // Nếu LAN → broadcast
        if (roomType == "lan")
            LanDiscovery.Instance?.StartBroadcast(pendingRoom);

        statusText.text = "Đang tạo phòng...";

        // Đăng ký callback rồi load Room scene qua NetworkManager
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnRoomSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene(roomScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    void OnRoomSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (sceneName == roomScene)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnRoomSceneLoaded;
    }

    string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }
}
