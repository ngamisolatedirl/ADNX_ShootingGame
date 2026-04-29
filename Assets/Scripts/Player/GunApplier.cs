using UnityEngine;

public class GunApplier : MonoBehaviour
{
    private Shooting shooting;

    void Start()
    {
        shooting = GetComponent<Shooting>();
        Apply();
    }

    public void Apply()
    {
        if (DataManager.Instance == null) return;

        GunData gun = DataManager.Instance.GetActiveGun();
        if (gun == null) return;

        // Apply fireRate
        if (shooting != null)
        {
            shooting.fireRate = gun.fireRate;
            shooting.damage = gun.damage;
            shooting.piercing = gun.piercing;
        }

        Debug.Log($"Applied gun → {gun.name} | Damage: {gun.damage} | FireRate: {gun.fireRate} | Piercing: {gun.piercing}");
    }
}