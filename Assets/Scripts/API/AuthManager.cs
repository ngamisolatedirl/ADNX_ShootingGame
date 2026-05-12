using System;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    // Token và thông tin user sau khi login
    public static string Token { get; private set; } = "";
    public static int UserId { get; private set; } = 0;
    public static string Username { get; private set; } = "";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tự động load token đã lưu từ lần trước
        Token = PlayerPrefs.GetString("auth_token", "");
        UserId = PlayerPrefs.GetInt("auth_userId", 0);
        Username = PlayerPrefs.GetString("auth_username", "");
    }

    // Đăng ký
    public async void Register(string username, string password,
        Action onSuccess, Action<string> onError)
    {
        try
        {
            var body = new RegisterRequest { username = username, password = password };
            string json = await ApiClient.Instance.Post("/auth/register", body);
            Debug.Log("[Auth] Đăng ký thành công");
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auth] Đăng ký lỗi: " + e.Message);
            onError?.Invoke(ParseError(e.Message));
        }
    }

    // Đăng nhập
    public async void Login(string username, string password,
        Action onSuccess, Action<string> onError)
    {
        try
        {
            var body = new LoginRequest { username = username, password = password };
            string json = await ApiClient.Instance.Post("/auth/login", body);

            // Parse response
            var res = JsonUtility.FromJson<LoginResponse>(json);

            // Lưu token
            Token = res.token;
            UserId = res.userId;
            Username = res.username;

            PlayerPrefs.SetString("auth_token", Token);
            PlayerPrefs.SetInt("auth_userId", UserId);
            PlayerPrefs.SetString("auth_username", Username);
            PlayerPrefs.Save();

            // Load save data vào DataManager
            DataManager.EnsureExists();
            DataManager.Instance.LoadFromServer(res.saveData);

            Debug.Log($"[Auth] Login OK: {Username}");
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auth] Login lỗi: " + e.Message);
            onError?.Invoke(ParseError(e.Message));
        }
    }

    public void Logout()
    {
        Token = "";
        UserId = 0;
        Username = "";
        PlayerPrefs.DeleteKey("auth_token");
        PlayerPrefs.DeleteKey("auth_userId");
        PlayerPrefs.DeleteKey("auth_username");
        PlayerPrefs.Save();
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    string ParseError(string raw)
    {
        // Lấy message từ JSON error nếu có
        try
        {
            var err = JsonUtility.FromJson<ErrorResponse>(raw);
            return err?.message ?? raw;
        }
        catch { return raw; }
    }

    // ── Request/Response models ────────────────────────────────────

    [Serializable] class RegisterRequest { public string username; public string password; }
    [Serializable] class LoginRequest { public string username; public string password; }
    [Serializable] class ErrorResponse { public string message; }

    [Serializable]
    public class LoginResponse
    {
        public string token;
        public int userId;
        public string username;
        public SaveData saveData;
    }
    public async void VerifyToken(Action onSuccess, Action onFail)
    {
        if (string.IsNullOrEmpty(Token))
        {
            onFail?.Invoke();
            return;
        }

        try
        {
            string json = await ApiClient.Instance.Get("/player/verify");
            var res = JsonUtility.FromJson<VerifyResponse>(json);

            // Cập nhật data mới nhất từ server
            UserId = res.userId;
            Username = res.username;
            DataManager.EnsureExists();
            DataManager.Instance.LoadFromServer(res.saveData);

            Debug.Log($"[Auth] Token hợp lệ: {Username}");
            onSuccess?.Invoke();
        }
        catch
        {
            // Token hết hạn hoặc server không phản hồi → xóa token cũ
            Debug.LogWarning("[Auth] Token không hợp lệ, yêu cầu đăng nhập lại");
            Logout();
            onFail?.Invoke();
        }
    }

    [Serializable]
    class VerifyResponse
    {
        public int userId;
        public string username;
        public SaveData saveData;
    }
}