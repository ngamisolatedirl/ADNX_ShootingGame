using UnityEngine;

/// <summary>
/// Đặt Camera này trực tiếp trong scene (không phải prefab, không phải con của player).
/// Khi player owner spawn xong, PlayerCameraController sẽ gọi SetTarget().
/// Trước đó camera đứng yên tại vị trí đặt trong scene.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public Vector2 offset = Vector2.zero;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Transform currentTarget;
    private bool isFollowing = false;

    void LateUpdate()
    {
        // Chưa có target → đứng yên ở vị trí scene
        if (!isFollowing || currentTarget == null) return;

        // Chỉ follow trục X, Y giữ nguyên vị trí camera đặt trong scene
        Vector3 targetPos = new Vector3(
            currentTarget.position.x + offset.x,
            transform.position.y,          // Y cố định
            transform.position.z
        );

        if (useBounds)
            targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Gọi bởi PlayerCameraController khi player owner spawn xong.
    /// Snap ngay đến vị trí player rồi bắt đầu follow mượt.
    /// </summary>
    public void SetTarget(Transform t)
    {
        if (t == null) return;

        currentTarget = t;

        // Snap X ngay đến vị trí player, giữ nguyên Y của camera
        transform.position = new Vector3(
            t.position.x + offset.x,
            transform.position.y,          // Y giữ nguyên
            transform.position.z
        );

        isFollowing = true;
        Debug.Log($"[Camera] Bắt đầu follow: {t.name}");
    }

    /// <summary>
    /// Gọi khi player chết → spectate người còn sống gần nhất.
    /// </summary>
    public void FindNearestAlivePlayer()
    {
        var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var ph in allPlayers)
        {
            if (ph == null || ph.IsDead) continue;
            float dist = Vector2.Distance(transform.position, ph.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = ph.transform;
            }
        }

        if (nearest != null)
        {
            currentTarget = nearest;
            Debug.Log($"[Camera] Spectating: {nearest.name}");
        }
    }

    public void StopFollowing() => isFollowing = false;
    public Transform CurrentTarget => currentTarget;
}