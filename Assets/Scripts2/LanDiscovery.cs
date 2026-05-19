using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LanDiscovery : MonoBehaviour
{
    public static LanDiscovery Instance { get; private set; }

    [Header("UDP Settings")]
    public int broadcastPort = 47777;
    public float broadcastInterval = 1.5f;

    // ── State ──────────────────────────────────────────────────────────────
    private UdpClient udpBroadcaster;
    private UdpClient udpListener;
    private Thread listenerThread;
    private volatile bool isListening = false;  
    private bool isBroadcasting = false;

    private RoomInfo hostedRoom;
    private float broadcastTimer = 0f;

    // Dùng long (milliseconds) thay float để thread-safe
    private readonly Dictionary<string, (RoomInfo info, long timestamp)> discoveredRooms
        = new Dictionary<string, (RoomInfo, long)>();
    private readonly object roomLock = new object();

    // Timeout 5 giây = 5000ms
    private const long ROOM_TIMEOUT_MS = 5000L;

    // Flag báo main thread có phòng mới cần fire event
    private volatile bool roomListDirty = false;

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

        // Client: fire event trên main thread khi background thread báo có data mới
        if (isListening)
        {
            if (roomListDirty)
            {
                roomListDirty = false;
                FireRoomListUpdate();
            }
            CleanupTimedOut();
        }
    }

    // ── HOST ───────────────────────────────────────────────────────────────

    public void StartBroadcast(RoomInfo room)
    {
        hostedRoom = room;
        isBroadcasting = true;
        broadcastTimer = broadcastInterval; // broadcast ngay lần đầu
        Debug.Log($"[LAN] Bắt đầu broadcast: {room.roomId}");
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
            Debug.Log($"[LAN] Broadcast gửi {data.Length} bytes đến port {broadcastPort}");
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
        isListening = false;        // signal thread tự thoát sau ReceiveTimeout
        udpListener?.Close();       // unblock Receive() ngay lập tức
        udpListener = null;
        
        // Thread sẽ tự thoát sau tối đa 1s (ReceiveTimeout)
        lock (roomLock) discoveredRooms.Clear();
        Debug.Log("[LAN] Dừng lắng nghe");
    }

    private void ListenLoop()
    {
        try
        {
            udpListener = new UdpClient(broadcastPort);
            udpListener.Client.ReceiveTimeout = 1000; // 1s timeout để check isListening
            Debug.Log($"[LAN] Listener đang lắng nghe trên port {broadcastPort}");

            while (isListening)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpListener.Receive(ref remote);
                    string json = Encoding.UTF8.GetString(data);
                    RoomInfo room = JsonUtility.FromJson<RoomInfo>(json);
                    Debug.Log($"[LAN] Nhận được packet từ {remote.Address}");

                    if (room != null && !string.IsNullOrEmpty(room.roomId))
                    {
                        // Ghi đè IP thực từ sender
                        room.hostIP = remote.Address.ToString();
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                        lock (roomLock)
                            discoveredRooms[room.roomId] = (room, now);

                        // Báo main thread có data mới để fire event
                        roomListDirty = true;
                    }
                }
                catch (SocketException) { /* timeout 1s, bình thường */ }
            }
        }
        catch (Exception e)
        {
            if (isListening)
                Debug.LogWarning($"[LAN] Listener lỗi: {e.Message}");
        }
        Debug.Log("[LAN] ListenLoop thoát");
    }

    // Dọn phòng timeout trên main thread (an toàn)
    private void CleanupTimedOut()
    {
        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        bool changed = false;

        lock (roomLock)
        {
            var toRemove = new List<string>();
            foreach (var kv in discoveredRooms)
            {
                if (now - kv.Value.timestamp > ROOM_TIMEOUT_MS)
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