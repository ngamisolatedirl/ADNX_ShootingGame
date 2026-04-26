using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    [Header("Brightness Buttons")]
    public Button btnDark;
    public Button btnNormal;
    public Button btnBright;

    [Header("Volume")]
    public Slider volumeSlider;

    void Start()
    {
        Debug.Log("OptionsManager Start");
        Debug.Log("btnDark: " + btnDark);
        Debug.Log("btnNormal: " + btnNormal);
        Debug.Log("btnBright: " + btnBright);
        Debug.Log("volumeSlider: " + volumeSlider);

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
            AudioListener.volume = volumeSlider.value;
            volumeSlider.onValueChanged.AddListener(ApplyVolume);
        }

        if (btnDark != null) btnDark.onClick.AddListener(() => ApplyBrightness(0.3f));
        if (btnNormal != null) btnNormal.onClick.AddListener(() => ApplyBrightness(1f));
        if (btnBright != null) btnBright.onClick.AddListener(() => ApplyBrightness(1.8f));
    }

    void ApplyBrightness(float value)
    {
        Debug.Log("ApplyBrightness: " + value);
        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
    }

    void ApplyVolume(float value)
    {
        Debug.Log("ApplyVolume: " + value);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    public void BackToMenu()
    {
        Debug.Log("BackToMenu");
        SceneManager.LoadScene("MainMenu");
    }
}