using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;
    public Button loginButton;
    public Button registerButton;
    public Button guestButton;

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        loginButton.onClick.AddListener(OnLogin);
        registerButton.onClick.AddListener(OnRegister);
        guestButton.onClick.AddListener(OnGuest);

        statusText.text = "";

        // Có token cũ → verify với server trước, không nhảy thẳng
        if (AuthManager.Instance.IsLoggedIn)
        {
            statusText.text = "Đang xác thực...";
            SetInteractable(false);

            AuthManager.Instance.VerifyToken(
                onSuccess: () =>
                {
                    statusText.text = $"Chào mừng trở lại, {AuthManager.Username}!";
                    Invoke(nameof(GoToMainMenu), 0.5f);
                },
                onFail: () =>
                {
                    statusText.text = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại";
                    SetInteractable(true);
                }
            );
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (usernameInput.isFocused)
                passwordInput.Select();
            else if (passwordInput.isFocused)
                usernameInput.Select();
        }
    }

    void OnLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Vui lòng nhập đầy đủ thông tin";
            return;
        }

        SetInteractable(false);
        statusText.text = "Đang đăng nhập...";

        AuthManager.Instance.Login(username, password,
            onSuccess: () =>
            {
                statusText.text = $"Xin chào, {AuthManager.Username}!";
                Invoke(nameof(GoToMainMenu), 0.5f);
            },
            onError: (msg) =>
            {
                statusText.text = "Lỗi: " + msg;
                SetInteractable(true);
            }
        );
    }

    void OnRegister()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Vui lòng nhập đầy đủ thông tin";
            return;
        }

        if (password.Length < 6)
        {
            statusText.text = "Password phải ít nhất 6 ký tự";
            return;
        }

        SetInteractable(false);
        statusText.text = "Đang đăng ký...";

        AuthManager.Instance.Register(username, password,
            onSuccess: () =>
            {
                statusText.text = "Đăng ký thành công! Đang đăng nhập...";
                AuthManager.Instance.Login(username, password,
                    onSuccess: () =>
                    {
                        statusText.text = $"Xin chào, {AuthManager.Username}!";
                        Invoke(nameof(GoToMainMenu), 0.5f);
                    },
                    onError: (msg) =>
                    {
                        statusText.text = "Lỗi: " + msg;
                        SetInteractable(true);
                    }
                );
            },
            onError: (msg) =>
            {
                statusText.text = "Lỗi: " + msg;
                SetInteractable(true);
            }
        );
    }

    void OnGuest()
    {
        DataManager.EnsureExists();
        GoToMainMenu();
    }

    void SetInteractable(bool value)
    {
        loginButton.interactable = value;
        registerButton.interactable = value;
        guestButton.interactable = value;
        usernameInput.interactable = value;
        passwordInput.interactable = value;
    }

    void GoToMainMenu() => SceneManager.LoadScene(mainMenuScene);
}