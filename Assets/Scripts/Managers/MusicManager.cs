using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private AudioSource audioSource;

    [Header("Music per Scene")]
    public AudioClip mainMenuMusic;
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;
    public AudioClip optionsMusic;
    public AudioClip creditsMusic;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply volume đã lưu
        audioSource.volume = PlayerPrefs.GetFloat("Volume", 1f);

        switch (scene.name)
        {
            case "MainMenu":
                PlayMusic(mainMenuMusic);
                break;
            case "Level1":
                PlayMusic(level1Music);
                break;
            case "Level2":
                PlayMusic(level2Music);
                break;
            case "Level3":
                PlayMusic(level3Music);
                break;
            case "Options":
                PlayMusic(optionsMusic);
                break;
            case "Credits":
                PlayMusic(creditsMusic);
                break;
        }
    }

    void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip) return; // Đang chạy rồi thì không restart

        audioSource.clip = clip;
        audioSource.Play();
    }
}