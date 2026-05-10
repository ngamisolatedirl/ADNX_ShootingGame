using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Gắn vào Player Prefab cùng với CharacterApplier, CostumeApplier, GunApplier.
///
/// Online: script này sync characterId, costumeId, gunId qua NetworkVariable.
///   - Owner đọc DataManager local → set NetworkVariable khi spawn
///   - Tất cả máy nhận OnValueChanged → lookup animator từ CharacterAnimatorConfig → apply
///
/// Offline: script này không làm gì, CharacterApplier/CostumeApplier/GunApplier
///   chạy bình thường như cũ.
///
/// Setup:
///   1. Gắn script này vào Player Prefab
///   2. Gán animatorConfig (cùng asset đang dùng trong CharacterApplier)
///   3. Thêm 1 dòng đầu Start() trong CharacterApplier, CostumeApplier, GunApplier:
///      if (NetworkUtils.IsOnline) return;
/// </summary>
public class PlayerAppearanceSync : NetworkBehaviour
{
    [Header("Config")]
    [Tooltip("Cùng CharacterAnimatorConfig asset đang dùng trong CharacterApplier")]
    public CharacterAnimatorConfig animatorConfig;

    // Sync 3 string ID từ owner xuống tất cả client
    private NetworkVariable<Unity.Collections.FixedString64Bytes> netCharacterId =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Unity.Collections.FixedString64Bytes> netCostumeId =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Unity.Collections.FixedString64Bytes> netGunId =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Animator animator;

    public override void OnNetworkSpawn()
    {
        // Offline: không làm gì, để Applier cũ xử lý
        if (!NetworkUtils.IsOnline) return;

        animator = GetComponentInChildren<Animator>();

        // Subscribe để nhận update khi giá trị thay đổi
        netCharacterId.OnValueChanged += OnAppearanceChanged;
        netCostumeId.OnValueChanged += OnAppearanceChanged;

        if (IsOwner)
        {
            // Đọc data của chính mình từ DataManager local
            if (DataManager.Instance == null)
            {
                Debug.LogWarning("[AppearanceSync] DataManager null!");
                return;
            }

            var saveData = DataManager.Instance.GetSaveData();
            var charSave = DataManager.Instance.GetActiveCharacterSaveData();

            // Set NetworkVariable → tự động sync xuống tất cả máy
            netCharacterId.Value = saveData.activeCharacterId ?? "cowboy";
            netCostumeId.Value = charSave?.activeCostumeId ?? "cowboy_default";
            netGunId.Value = saveData.activeGunId ?? "pistol";

            Debug.Log($"[AppearanceSync] Owner set: char={netCharacterId.Value} costume={netCostumeId.Value} gun={netGunId.Value}");
        }

        // Apply ngay lần đầu (cho cả owner lẫn non-owner nhận giá trị hiện tại)
        ApplyAppearance();
    }

    public override void OnNetworkDespawn()
    {
        netCharacterId.OnValueChanged -= OnAppearanceChanged;
        netCostumeId.OnValueChanged -= OnAppearanceChanged;
    }

    // Callback khi NetworkVariable thay đổi
    private void OnAppearanceChanged(
        Unity.Collections.FixedString64Bytes oldVal,
        Unity.Collections.FixedString64Bytes newVal)
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (animatorConfig == null)
        {
            Debug.LogWarning("[AppearanceSync] animatorConfig chưa được gán!");
            return;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        string charId = netCharacterId.Value.ToString();
        string costumeId = netCostumeId.Value.ToString();

        // Bỏ qua nếu chưa có data (lần đầu spawn trước khi owner set)
        if (string.IsNullOrEmpty(charId)) return;

        RuntimeAnimatorController controller = animatorConfig.GetController(charId, costumeId);

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log($"[AppearanceSync] Applied: char={charId} costume={costumeId} trên {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[AppearanceSync] Không tìm thấy controller cho char={charId} costume={costumeId}");
        }

        // Apply gun stats nếu là owner (chỉ owner cần fireRate, damage, piercing)
        if (IsOwner) ApplyGunStats();
    }

    private void ApplyGunStats()
    {
        string gunId = netGunId.Value.ToString();
        if (string.IsNullOrEmpty(gunId)) return;

        GunData gun = DataManager.Instance?.GetGunData(gunId);
        if (gun == null) return;

        var shooting = GetComponent<Shooting>();
        if (shooting != null)
        {
            shooting.fireRate = gun.fireRate;
            shooting.damage = gun.damage;
            shooting.piercing = gun.piercing;
            Debug.Log($"[AppearanceSync] Gun applied: {gun.name}");
        }
    }
}