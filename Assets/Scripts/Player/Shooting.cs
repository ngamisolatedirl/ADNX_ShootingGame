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

    [Header("Gun Stats")]
    public float damage = 10f;
    public bool piercing = false;

    void Update()
    {
        // QUAN TRỌNG: Chỉ chủ sở hữu (Owner) mới được xử lý phím bấm
        // Dù Offline (Host) hay Online (Client), IsOwner đều sẽ hoạt động đúng
        if (!IsOwner) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        fireTimer += Time.deltaTime;
        bool canShoot = fireTimer >= 1f / fireRate;

        if (canShoot)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Shoot(new Vector2(GetFacingDir(), 0));
            }
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                Shoot(Vector2.down);
            }
            else if (keyboard.xKey.wasPressedThisFrame || keyboard.ctrlKey.wasPressedThisFrame)
            {
                Shoot(new Vector2(GetFacingDir(), -1f).normalized);
            }
        }
    }

    float GetFacingDir()
    {
        // Lấy hướng dựa trên scale của nhân vật
        return transform.root.localScale.x > 0 ? 1f : -1f;
    }

    void Shoot(Vector2 dir)
    {
        fireTimer = 0f;

        // Tính toán sát thương (Tính ở máy khách để phản hồi nhanh, nhưng Server sẽ nhận giá trị này)
        float finalDamage = damage;
        if (DataManager.Instance != null)
        {
            var saveData = DataManager.Instance.GetSaveData();
            BaseStats stats = DataManager.Instance.GetComputedStats(saveData.activeCharacterId);
            if (stats != null && Random.value < stats.critRate)
            {
                finalDamage *= stats.critDamage;
            }
        }

        // Chạy animation local ngay lập tức để không bị delay cảm giác
        if (TryGetComponent<PlayerAnimator>(out var anim)) anim.PlayShoot();

        // GỬI LỆNH LÊN SERVER (Dù chơi 1 mình hay nhiều mình đều qua đây)
        SpawnBulletServerRpc(shootingPoint.position, dir, finalDamage, piercing);
    }

    [ServerRpc]
    void SpawnBulletServerRpc(Vector3 position, Vector2 dir, float finalDamage, bool isPiercing)
    {
        // 1. Server sinh ra viên đạn
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

        // 2. Gán thông số đạn (Server làm việc này)
        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            bulletScript.SetDirection(dir);
            bulletScript.SetStats(finalDamage, isPiercing);
        }

        // 3. QUAN TRỌNG: Spawn lên toàn Network
        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
    }
}