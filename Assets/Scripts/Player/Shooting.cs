using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Shooting : NetworkBehaviour
{
    [Header("Setup")]
    public Transform shootingPoint;
    public GameObject bulletPrefab;

    [Header("Fire Rate")]
    public float fireRate = 3f;
    private float fireTimer = 0f;

    [Header("Gun Stats (set by GunApplier)")]
    public float damage = 10f;
    public bool piercing = false;

    void Update()
    {
        // Nếu đang online mà không phải chủ sở hữu thì không xử lý input
        if (NetworkUtils.IsOnline && !IsOwner) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        fireTimer += Time.deltaTime;
        bool canShoot = fireTimer >= 1f / fireRate;

        // Input bắn ngang (Space)
        if (keyboard.spaceKey.wasPressedThisFrame && canShoot)
        {
            ShootHorizontal();
            ResetTimer();
        }
        // Input bắn xuống (S hoặc Down Arrow)
        else if ((keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) && canShoot)
        {
            ShootDown();
            ResetTimer();
        }
        // Input bắn chéo xuống (X hoặc Ctrl)
        else if ((keyboard.xKey.wasPressedThisFrame || keyboard.ctrlKey.wasPressedThisFrame) && canShoot)
        {
            ShootDiagonalDown();
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        fireTimer = 0f;
        // Chạy animation bắn
        if (TryGetComponent<PlayerAnimator>(out var anim))
        {
            anim.PlayShoot();
        }
    }

    void ShootHorizontal()
    {
        // Xác định hướng dựa trên scale của nhân vật (lấy từ root hoặc chính nó)
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
        if (shootingPoint == null)
        {
            Debug.LogError("Shooting: Chưa gán shootingPoint trên Inspector!");
            return;
        }

        // 1. Tính toán sát thương và Crit từ DataManager
        float finalDamage = damage;
        if (DataManager.Instance != null)
        {
            var saveData = DataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                BaseStats stats = DataManager.Instance.GetComputedStats(saveData.activeCharacterId);
                if (stats != null && Random.value < stats.critRate)
                {
                    finalDamage *= stats.critDamage;
                    Debug.Log($"<color=red>CRIT!</color> Damage: {finalDamage}");
                }
            }
        }

        // 2. Thực hiện Spawn theo chế độ chơi
        if (NetworkUtils.IsOnline)
        {
            // Chế độ Online: Gửi yêu cầu lên Server
            SpawnBulletServerRpc(shootingPoint.position, dir, finalDamage, piercing);
        }
        else
        {
            // Chế độ Offline: Tạo trực tiếp bằng Instantiate truyền thống
            LocalSpawn(shootingPoint.position, dir, finalDamage, piercing);
        }
    }

    // Hàm bổ trợ cho việc spawn máy đơn
    void LocalSpawn(Vector3 pos, Vector2 dir, float dmg, bool isPiercing)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            bulletScript.SetDirection(dir);
            bulletScript.SetStats(dmg, isPiercing);
        }
    }

    [ServerRpc]
    void SpawnBulletServerRpc(Vector3 position, Vector2 dir, float finalDamage, bool isPiercing)
    {
        // Server tạo object
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            // Set thông số TRƯỚC khi Spawn lên Network
            bulletScript.SetDirection(dir);
            bulletScript.SetStats(finalDamage, isPiercing);
        }

        // Đưa object lên hệ thống Network để đồng bộ với tất cả Client
        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
    }
}