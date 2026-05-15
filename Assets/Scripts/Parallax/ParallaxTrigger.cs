using UnityEngine;

public class ParallaxTrigger : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] float parallaxFactor = 0.5f;
    [SerializeField] float triggerDistance = 10f;

    private Vector3 lastCamPos;

    void Start() => lastCamPos = cam.position;

    void LateUpdate()
    {
        float distanceToCam = Vector2.Distance(transform.position, cam.position);

        Vector3 delta = cam.position - lastCamPos;

        if (distanceToCam <= triggerDistance)
        {
            transform.position += delta * (1 - parallaxFactor);
        }

        lastCamPos = cam.position;
    }
}