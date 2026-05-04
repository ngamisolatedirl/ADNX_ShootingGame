using UnityEngine;
using UnityEngine.InputSystem;

public class Shooting : MonoBehaviour
{
    public Transform shootingPoint;
    public GameObject bulletPrefab;

    [Header("Fire Rate")]
    public float fireRate = 3f; // Số phát mỗi giây
    private float fireTimer = 0f;

    [Header("Gun Stats (set by GunApplier)")]
    public float damage = 10f;
    public bool piercing = false;
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        fireTimer += Time.deltaTime;

        bool canShoot = fireTimer >= 1f / fireRate;

        if (keyboard.spaceKey.wasPressedThisFrame && canShoot)
        {
            ShootHorizontal();
            ResetTimer();
        }
        else if ((keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) && canShoot)
        {
            ShootDown();
            ResetTimer();
        }
        else if ((keyboard.xKey.wasPressedThisFrame || keyboard.ctrlKey.wasPressedThisFrame) && canShoot)
        {
            ShootDiagonalDown();
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        fireTimer = 0f;
        GetComponent<PlayerAnimator>().PlayShoot();
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
        SpawnBullet(new Vector2(dirX, -1f).normalized);
    }

    void SpawnBullet(Vector2 dir)
    {
        // Tính crit
        BaseStats stats = DataManager.Instance.GetComputedStats(
            DataManager.Instance.GetSaveData().activeCharacterId
        );

        float finalDamage = damage;
        if (stats != null && Random.value < stats.critRate)
        {
            finalDamage *= stats.critDamage;
            Debug.Log($"CRIT! Damage: {finalDamage}");
        }

        GameObject bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetDirection(dir);
        bulletScript.SetStats(finalDamage, piercing);
    }
}