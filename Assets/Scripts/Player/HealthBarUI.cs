using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;
    public float animDuration = 0.3f;

    [Header("Color")]
    public Color fullColor = Color.green;
    public Color lowColor = Color.red;
    public float lowThreshold = 0.3f;

    private PlayerHealth playerHealth;
    private float targetFill = 1f;
    private float currentFill = 1f;

    // Instance ID của player đang bind để tránh bind lại player cũ
    private int boundInstanceID = -1;

    void Start()
    {
        SetFillImmediate(1f);
        StartCoroutine(FindPlayerRoutine());
    }

    void OnDestroy()
    {
        Unbind();
    }

    // Reset từ GameManager gọi khi restart
    public void ResetToFull()
    {
        Unbind();
        boundInstanceID = -1;
        SetFillImmediate(1f);
        StopAllCoroutines();
        StartCoroutine(FindPlayerRoutine());
    }

    System.Collections.IEnumerator FindPlayerRoutine()
    {
        // Chờ scene transition xong hoàn toàn
        // Dùng WaitForSecondsRealtime để không bị ảnh hưởng bởi timeScale = 0
        yield return new WaitForSecondsRealtime(0.5f);

        float elapsed = 0f;
        float timeout = 15f;

        while (elapsed < timeout)
        {
            PlayerHealth found = FindLocalPlayer();

            if (found != null && found.GetInstanceID() != boundInstanceID)
            {
                Unbind();
                BindPlayer(found);
                yield break;
            }

            elapsed += 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        Debug.LogWarning("[HealthBarUI] Timeout - không tìm được player sau 15s");
    }

    // Sửa FindLocalPlayer — bỏ qua player đã chết
    PlayerHealth FindLocalPlayer()
    {
        foreach (var ph in FindObjectsOfType<PlayerHealth>())
        {
            if (ph == null || !ph.gameObject.activeInHierarchy) continue;
            if (ph.IsDead) continue; // ← thêm: bỏ qua player cũ HP=0

            if (!NetworkUtils.IsOnline)
                return ph;

            var netObj = ph.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner && netObj.IsSpawned)
                return ph;
        }
        return null;
    }

    void BindPlayer(PlayerHealth ph)
    {
        playerHealth = ph;
        boundInstanceID = ph.GetInstanceID();
        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerHealth.OnDied += HandlePlayerDied; // ← thêm
        SetFillImmediate(1f);
        Debug.Log($"[HealthBarUI] Bind player instanceID={boundInstanceID}");
    }

    void Unbind()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnDied -= HandlePlayerDied; // ← thêm
            playerHealth = null;
        }
    }

    void HandlePlayerDied()
    {
        Unbind(); // Ngắt kết nối ngay, không chờ FindPlayerRoutine
    }

    void HandleHealthChanged(float oldHP, float newHP, float maxHP)
    {
        targetFill = maxHP > 0 ? Mathf.Clamp01(newHP / maxHP) : 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(currentFill, targetFill)) return;

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.unscaledDeltaTime / animDuration);
        if (Mathf.Abs(currentFill - targetFill) < 0.001f)
            currentFill = targetFill;

        ApplyFill(currentFill);
    }

    void SetFillImmediate(float ratio)
    {
        currentFill = ratio;
        targetFill = ratio;
        ApplyFill(ratio);
    }

    void ApplyFill(float ratio)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = ratio;

        float t = ratio <= lowThreshold
            ? 0f
            : (ratio - lowThreshold) / (1f - lowThreshold);

        fillImage.color = Color.Lerp(lowColor, fullColor, t);
    }
}