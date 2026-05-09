using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attach vào Player Prefab.
///
/// Flow:
/// 1. Player spawn → OnNetworkSpawn chạy
/// 2. Nếu IsOwner → tìm Camera trong scene (Camera.main hoặc tag "LocalCamera")
/// 3. Gọi CameraFollow.SetTarget(transform) → camera bắt đầu follow player này
///
/// Non-owner không làm gì → camera của máy đó không bị ảnh hưởng.
///
/// Setup trong Unity:
/// - Đặt 1 Camera trong mỗi Level scene, tag = "MainCamera"
/// - Gắn CameraFollow vào Camera đó
/// - Gắn PlayerCameraController vào Player Prefab (không cần gán gì trong Inspector)
/// </summary>
public class PlayerCameraController : NetworkBehaviour
{
    // Tham chiếu đến camera local của máy này (sau khi đã tìm được)
    private CameraFollow localCamera;

    public override void OnNetworkSpawn()
    {
        // Chỉ owner mới cần gắn camera
        // Host: IsOwner = true với player của mình
        // Client: IsOwner = true chỉ với player của chính họ
        if (!IsOwner) return;

        // Dùng coroutine để đảm bảo scene đã load xong trước khi tìm camera
        StartCoroutine(FindAndAttachCamera());
    }

    private System.Collections.IEnumerator FindAndAttachCamera()
    {
        // Chờ đến khi active scene là Level scene thật sự (không phải LevelSelect hay DontDestroyOnLoad)
        string[] levelScenes = { "Level1", "Level2", "Level3", "Level4" };

        while (true)
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isLevelScene = System.Array.IndexOf(levelScenes, activeScene) >= 0;

            if (isLevelScene) break;

            yield return null;
        }

        // Thêm 1 frame nữa để Camera trong Level scene kịp Awake/Start
        yield return null;

        localCamera = Camera.main?.GetComponent<CameraFollow>();

        if (localCamera == null)
            localCamera = FindFirstObjectByType<CameraFollow>();

        if (localCamera == null)
        {
            Debug.LogError("[PlayerCamCtrl] Không tìm thấy CameraFollow trong scene!");
            yield break;
        }

        localCamera.SetTarget(this.transform);
        Debug.Log($"[PlayerCamCtrl] Player {OwnerClientId} gắn camera thành công trong scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    /// <summary>
    /// Gọi từ PlayerHealth khi player chết.
    /// Camera sẽ chuyển sang spectate người còn sống.
    /// </summary>
    public void OnPlayerDied()
    {
        if (!IsOwner) return;
        localCamera?.FindNearestAlivePlayer();
    }

    /// <summary>
    /// Trả về CameraFollow của máy này (dùng cho các script khác nếu cần).
    /// </summary>
    public CameraFollow GetLocalCamera() => localCamera;
}