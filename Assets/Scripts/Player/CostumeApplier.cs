using UnityEngine;

public class CostumeApplier : MonoBehaviour
{
    public RuntimeAnimatorController baseController;
    public AnimatorOverrideController[] costumeControllers; 

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        Apply();
    }

    public void Apply()
    {
        if (DataManager.Instance == null) return;

        CharacterSaveData save = DataManager.Instance.GetActiveCharacterSaveData();
        if (save == null) return;

        string costumeId = save.activeCostumeId;

        // Tìm index costume trong config
        CharacterData config = DataManager.Instance.GetActiveCharacter();
        int index = config.costumes.FindIndex(c => c.id == costumeId);

        if (index >= 0 && index < costumeControllers.Length)
        {
            animator.runtimeAnimatorController = costumeControllers[index];
            Debug.Log("Applied costume: " + costumeId);
        }
        else
        {
            animator.runtimeAnimatorController = baseController;
            Debug.Log("Dùng costume mặc định");
        }
    }
}