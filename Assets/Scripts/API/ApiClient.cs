using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [Header("Server URL")]
    public string baseUrl = "http://localhost:5206";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // GET request
    public async Task<string> Get(string endpoint)
    {
        string url = baseUrl + endpoint;
        using var req = UnityWebRequest.Get(url);

        // Gắn token nếu đã login
        if (!string.IsNullOrEmpty(AuthManager.Token))
            req.SetRequestHeader("Authorization", "Bearer " + AuthManager.Token);

        await SendRequest(req);

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"GET {endpoint} lỗi: {req.error}");

        return req.downloadHandler.text;
    }

    // POST request
    public async Task<string> Post(string endpoint, object body)
    {
        string url = baseUrl + endpoint;
        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(AuthManager.Token))
            req.SetRequestHeader("Authorization", "Bearer " + AuthManager.Token);

        await SendRequest(req);

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"POST {endpoint} lỗi: {req.error} | {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    // Helper chờ request xong
    private Task SendRequest(UnityWebRequest req)
    {
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }
}