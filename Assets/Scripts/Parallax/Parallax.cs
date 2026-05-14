using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] float parallaxFactor = 0.5f; // 0 = theo cam, 1 = đứng yên

    private Vector3 lastCamPos;

    void Start() => lastCamPos = cam.position;

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += delta * (1 - parallaxFactor);
        lastCamPos = cam.position;
    }
}