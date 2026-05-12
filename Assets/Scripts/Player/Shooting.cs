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
        if (!IsOwner) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        fireTimer += Time.deltaTime;
        bool canShoot = fireTimer >= 1f / fireRate;

        if (canShoot)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
                Shoot(new Vector2(GetFacingDir(), 0));
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                Shoot(Vector2.down);
            else if (keyboard.xKey.wasPressedThisFrame || keyboard.ctrlKey.wasPressedThisFrame)
                Shoot(new Vector2(GetFacingDir(), -1f).normalized);
        }
    }

    float GetFacingDir()
    {
        return transform.root.localScale.x > 0 ? 1f : -1f;
    }

    void Shoot(Vector2 dir)
    {
        fireTimer = 0f;

        float finalDamage = damage;
        if (DataManager.Instance != null)
        {
            var saveData = DataManager.Instance.GetSaveData();
            BaseStats stats = DataManager.Instance.GetComputedStats(saveData.activeCharacterId);
            if (stats != null && Random.value < stats.critRate)
                finalDamage *= stats.critDamage;
        }

        if (TryGetComponent<PlayerAnimator>(out var anim))
            anim.PlayShoot();

        // Truyền thêm OwnerClientId để server biết viên đạn này của ai
        SpawnBulletServerRpc(shootingPoint.position, dir, finalDamage, piercing, OwnerClientId);
    }

    [ServerRpc]
    void SpawnBulletServerRpc(Vector3 position, Vector2 dir, float finalDamage, bool isPiercing, ulong ownerClientId)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            bulletScript.SetDirection(dir);
            bulletScript.SetStats(finalDamage, isPiercing);
            bulletScript.SetOwner(ownerClientId); // ← gán owner
        }

        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
            netObj.Spawn();
    }
}