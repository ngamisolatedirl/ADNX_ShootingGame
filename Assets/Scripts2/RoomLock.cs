using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Singleton persistent — tồn tại xuyên suốt mọi scene.
/// Chỉ làm 1 việc: giữ ConnectionApprovalCallback để chặn join sau khi game bắt đầu.
///
/// Cách dùng:
///   RoomLock.Lock();   // gọi khi host bấm Start (trong RoomManager)
///   RoomLock.Unlock(); // gọi khi về menu (trong GameManager.DoLocalLeave)
/// </summary>
public class RoomLock : MonoBehaviour
{
    public static RoomLock Instance { get; private set; }

    private bool locked = false;

    public static bool IsLocked => Instance != null && Instance.locked;

    // Tự tạo GameObject ngay lần đầu được gọi — không cần đặt vào scene nào
    private static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("RoomLock");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<RoomLock>();
        Debug.Log("[RoomLock] Instance created via EnsureExists");
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RoomLock] Duplicate detected — destroying self");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[RoomLock] Awake — instance ready");
    }

    public static void Lock()
    {
        EnsureExists();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RoomLock] Lock() — NetworkManager.Singleton is null!");
            return;
        }

        Instance.locked = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = Instance.ApprovalCheck;

        // LOG-B
        Debug.Log($"[LOG-B] After Lock(). locked={Instance.locked}, " +
                  $"callback={NetworkManager.Singleton.ConnectionApprovalCallback?.Method.Name ?? "null"}");
    }

    public static void Unlock()
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RoomLock] Unlock() — Instance is null, nothing to unlock");
            return;
        }

        Instance.locked = false;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

        Debug.Log("[RoomLock] UNLOCKED — callback cleared");
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // LOG-E
        Debug.Log($"[LOG-E] ApprovalCheck called. locked={locked}, approved={!locked}, " +
                  $"clientId={request.ClientNetworkId}");

        response.Approved = !locked;
        response.Reason = locked ? "Game đã bắt đầu rồi!" : "";
        response.Pending = false;
    }
}