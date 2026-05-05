using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GunConfig
{
    public List<GunData> guns;
}

[System.Serializable]
public class GunData
{
    public string id;
    public string name;
    public float damage;
    public float fireRate;
    public int price;
    public string bulletType;
    public bool piercing;
}

[System.Serializable]
public class CharacterConfig
{
    public List<CharacterData> characters;
}

[System.Serializable]
public class CharacterData
{
    public string id;
    public string name;
    public int price;
    public BaseStats baseStats;
    public UpgradeTable upgrades;
    public List<CostumeData> costumes;
    [System.NonSerialized] public RuntimeAnimatorController animatorOverride;
}

[System.Serializable]
public class BaseStats
{
    public float hp;
    public float speed;
    public float critRate;
    public float critDamage;
}

[System.Serializable]
public class UpgradeTable
{
    public UpgradeStat hp;
    public UpgradeStat speed;
    public UpgradeStat critRate;
    public UpgradeStat critDamage;
}

[System.Serializable]
public class UpgradeStat
{
    public int maxLevel;
    public List<int> costPerLevel;
    public float increasePerLevel;
}

[System.Serializable]
public class CostumeData
{
    public string id;
    public string name;
    public int price;
}

// ── Save Data ─────────────────────────────────────

[System.Serializable]
public class SaveData
{
    public int coins;
    public string activeCharacterId;
    public string activeGunId;
    public List<string> purchasedGuns;
    public List<CharacterSaveData> characters;
}

[System.Serializable]
public class CharacterSaveData
{
    public string id;
    public bool isPurchased;
    public string activeCostumeId;
    public List<string> purchasedCostumes;
    public UpgradeLevels upgradeLevels;
}

[System.Serializable]
public class UpgradeLevels
{
    public int hp;
    public int speed;
    public int critRate;
    public int critDamage;
}