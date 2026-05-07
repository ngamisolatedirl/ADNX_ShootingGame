using UnityEngine;

/// <summary>
/// Lưu dữ liệu tạm thời trong 1 session (coin, kill).
/// Chỉ flush vào DataManager khi win/lose → về lobby.
/// Dùng cho cả offline lẫn online.
/// </summary>
public class SessionData : MonoBehaviour
{
    public static SessionData Instance { get; private set; }

    // Coin kiếm được trong session này (chưa save vào DataManager)
    public int sessionCoins { get; private set; } = 0;
    public int sessionKills { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCoins(int amount)
    {
        sessionCoins += amount;
        Debug.Log($"[Session] +{amount} coins → total: {sessionCoins}");
    }

    public void AddKill()
    {
        sessionKills++;
    }

    /// <summary>
    /// Flush session data vào DataManager và reset.
    /// Gọi khi win hoặc lose trước khi về lobby.
    /// </summary>
    public void FlushAndReset()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddCoins(sessionCoins);
            Debug.Log($"[Session] Flush {sessionCoins} coins, {sessionKills} kills → DataManager");
        }
        sessionCoins = 0;
        sessionKills = 0;
    }

    public void Reset()
    {
        sessionCoins = 0;
        sessionKills = 0;
    }
}
