using UnityEngine;
using UnityEngine.InputSystem;

public class Shooting : MonoBehaviour
{
    public Transform shootingPoint;
    public GameObject bulletPrefab;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
            Bullet bulletScript = bullet.GetComponent<Bullet>();

            // Lấy hướng từ scale.x của player
            // scale.x > 0 = phải, scale.x < 0 = trái
            float dirX = transform.root.localScale.x > 0 ? 1f : -1f;
            bulletScript.SetDirection(new Vector2(dirX, 0));
        }
    }
}