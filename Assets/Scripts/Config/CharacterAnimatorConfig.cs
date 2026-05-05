using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterAnimatorConfig", menuName = "Config/CharacterAnimatorConfig")]
public class CharacterAnimatorConfig : ScriptableObject
{
    [System.Serializable]
    public class CostumeEntry
    {
        public string costumeId;
        public RuntimeAnimatorController animatorController;
    }

    [System.Serializable]
    public class CharacterEntry
    {
        public string characterId;
        public RuntimeAnimatorController defaultController;
        public List<CostumeEntry> costumes;

        public RuntimeAnimatorController GetController(string costumeId)
        {
            CostumeEntry entry = costumes.Find(c => c.costumeId == costumeId);
            return entry?.animatorController ?? defaultController;
        }
    }

    public List<CharacterEntry> characters;

    public RuntimeAnimatorController GetController(string characterId, string costumeId)
    {
        CharacterEntry entry = characters.Find(c => c.characterId == characterId);
        return entry?.GetController(costumeId) ?? null;
    }
}