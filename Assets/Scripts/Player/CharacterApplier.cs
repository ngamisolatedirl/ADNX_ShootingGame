using UnityEngine;

public class CharacterApplier : MonoBehaviour
{
    public CharacterAnimatorConfig animatorConfig;

    private Animator animator;

    void Start()
    {
        DataManager.EnsureExists(); // ← tự tạo nếu chưa có
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("DataManager character vẫn null!");
            return;
        }
        animator = GetComponent<Animator>();
        Apply();
    }

    public void Apply()
    {
        if (DataManager.Instance == null) return;

        string characterId = DataManager.Instance.GetSaveData().activeCharacterId;
        CharacterSaveData saveData = DataManager.Instance.GetCharacterSaveData(characterId);
        string costumeId = saveData?.activeCostumeId ?? characterId + "_default";

        Debug.Log($"characterId: {characterId} | costumeId: {costumeId}");

        RuntimeAnimatorController controller = animatorConfig.GetController(characterId, costumeId);
        Debug.Log($"Controller tìm được: {(controller != null ? controller.name : "NULL")}");

        if (controller != null)
            animator.runtimeAnimatorController = controller;

        PlayerStatsApplier statsApplier = GetComponent<PlayerStatsApplier>();
        if (statsApplier != null)
            statsApplier.Apply();
    }
}