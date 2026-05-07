using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// LAN Discovery dùng UDP broadcast.
/// Host: broadcast RoomInfo định kỳ.
/// Client: lắng nghe và thu thập danh sách phòng.
/// </summary>
public class LanDiscovery : MonoBehaviour
{
    public static LanDiscovery Instance { get; private set; }

    [Header("UDP Settings")]
    public int broadcastPort = 47777;
    public float broadcastInterval = 1.5f;  // host broadcast mỗi 1.5s

    // ── State ──────────────────────────────────────────────────────────────
    private UdpClient udpBroadcaster;
    private UdpClient udpListener;
    private Thread listenerThread;
    private bool isListening = false;
    private bool isBroadcasting = false;

    // RoomInfo của host hiện tại (nếu đang host)
    private RoomInfo hostedRoom;
    private float broadcastTimer = 0f;

    // Danh sách phòng tìm được (client side)
    private readonly Dictionary<string, (RoomInfo info, float timestamp)> discoveredRooms
        = new Dictionary<string, (RoomInfo, float)>();
    private readonly object roomLock = new object();

    // Timeout: xóa phòng nếu không nhận broadcast sau 5s
    private const float ROOM_TIMEOUT = 5f;

    // Event báo UI cập nhật
    public event Action<List<RoomInfo>> OnRoomListUpdated;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Host broadcast định kỳ
        if (isBroadcasting && hostedRoom != null)
        {
            broadcastTimer += Time.deltaTime;
            if (broadcastTimer >= broadcastInterval)
            {
                broadcastTimer = 0f;
                BroadcastRoom();
            }
        }

        // Client: dọn phòng timeout + fire event
        if (isListening)
            CleanupAndNotify();
    }

    // ── HOST ───────────────────────────────────────────────────────────────

    public void StartBroadcast(RoomInfo room)
    {
        hostedRoom = room;
        isBroadcasting = true;
        broadcastTimer = broadcastInterval; // broadcast ngay lập tức lần đầu
        Debug.Log($"[LAN] Bắt đầu broadcast phòng: {room.roomId}");
    }

    public void UpdateBroadcastRoom(RoomInfo room)
    {
        hostedRoom = room;
    }

    public void StopBroadcast()
    {
        isBroadcasting = false;
        hostedRoom = null;
        udpBroadcaster?.Close();
        udpBroadcaster = null;
        Debug.Log("[LAN] Dừng broadcast");
    }

    private void BroadcastRoom()
    {
        try
        {
            if (udpBroadcaster == null)
            {
                udpBroadcaster = new UdpClient();
                udpBroadcaster.EnableBroadcast = true;
            }

            string json = JsonUtility.ToJson(hostedRoom);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            udpBroadcaster.Send(data, data.Length, endpoint);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LAN] Broadcast lỗi: {e.Message}");
        }
    }

    // ── CLIENT ─────────────────────────────────────────────────────────────

    public void StartListening()
    {
        if (isListening) return;
        isListening = true;

        listenerThread = new Thread(ListenLoop) { IsBackground = true };
        listenerThread.Start();
        Debug.Log("[LAN] Bắt đầu lắng nghe phòng...");
    }

    public void StopListening()
    {
        isListening = false;
        udpListener?.Close();
        udpListener = null;
        listenerThread?.Abort();
        lock (roomLock) discoveredRooms.Clear();
        Debug.Log("[LAN] Dừng lắng nghe");
    }

    private void ListenLoop()
    {
        try
        {
            udpListener = new UdpClient(broadcastPort);
            udpListener.Client.ReceiveTimeout = 1000;

            while (isListening)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpListener.Receive(ref remote);
                    string json = Encoding.UTF8.GetString(data);
                    RoomInfo room = JsonUtility.FromJson<RoomInfo>(json);

                    if (room != null && !string.IsNullOrEmpty(room.roomId))
                    {
                        // Ghi đè IP thực từ sender (tránh host gửi sai IP)
                        room.hostIP = remote.Address.ToString();

                        lock (roomLock)
                            discoveredRooms[room.roomId] = (room, Time.realtimeSinceStartup);
                    }
                }
                catch (SocketException) { /* timeout, bình thường */ }
            }
        }
        catch (Exception e)
        {
            if (isListening)
                Debug.LogWarning($"[LAN] Listener lỗi: {e.Message}");
        }
    }

    private void CleanupAndNotify()
    {
        bool changed = false;
        float now = Time.realtimeSinceStartup;

        lock (roomLock)
        {
            var toRemove = new List<string>();
            foreach (var kv in discoveredRooms)
            {
                if (now - kv.Value.timestamp > ROOM_TIMEOUT)
                {
                    toRemove.Add(kv.Key);
                    changed = true;
                }
            }
            foreach (var key in toRemove)
                discoveredRooms.Remove(key);
        }

        if (changed) FireRoomListUpdate();
    }

    public void FireRoomListUpdate()
    {
        var list = new List<RoomInfo>();
        lock (roomLock)
            foreach (var kv in discoveredRooms.Values)
                list.Add(kv.info);
        OnRoomListUpdated?.Invoke(list);
    }

    public List<RoomInfo> GetDiscoveredRooms()
    {
        var list = new List<RoomInfo>();
        lock (roomLock)
            foreach (var kv in discoveredRooms.Values)
                list.Add(kv.info);
        return list;
    }

    void OnDestroy()
    {
        StopBroadcast();
        StopListening();
    }

    void OnApplicationQuit()
    {
        StopBroadcast();
        StopListening();
    }
}
