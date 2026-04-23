using UnityEngine;
using UnityEngine.InputSystem;

public class Shooting : MonoBehaviour
{
    public Transform shootingPoint;
    public GameObject bulletPrefab;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Bắn thẳng (space)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            ShootHorizontal();
        }

        // Bắn xuống thẳng (phím S hoặc DownArrow)
        if ((keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
        {
            ShootDown();
        }

        // Bắn chéo xuống (phím X hoặc Ctrl)
        if (keyboard.xKey.wasPressedThisFrame || keyboard.ctrlKey.wasPressedThisFrame)
        {
            ShootDiagonalDown();
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            float dirX = transform.root.localScale.x > 0 ? 1f : -1f;
            bulletScript.SetDirection(new Vector2(dirX, 0));

            // Gọi animation bắn
            GetComponent<PlayerAnimator>().PlayShoot();
        }
    }

    void ShootHorizontal()
    {
        float dirX = transform.root.localScale.x > 0 ? 1f : -1f;
        SpawnBullet(new Vector2(dirX, 0));
    }

    void ShootDown()
    {
        SpawnBullet(Vector2.down);
    }

    void ShootDiagonalDown()
    {
        float dirX = transform.root.localScale.x > 0 ? 1f : -1f;
        Vector2 diagonalDir = new Vector2(dirX, -1f).normalized; // góc 45 độ
        SpawnBullet(diagonalDir);
    }

    void SpawnBullet(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetDirection(dir);
    }
}
