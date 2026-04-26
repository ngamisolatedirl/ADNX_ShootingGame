using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BrightnessApplier : MonoBehaviour
{
    private Light2D globalLight;

    void Start()
    {
        globalLight = FindFirstObjectByType<Light2D>();
        if (globalLight == null) return;

        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1f);
        globalLight.intensity = savedBrightness;
    }
}