using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;

public enum VFX
{
    None, VerticalSlash, HorizontalSlash, Smash, SpinSlash, FireBreath, SkyRipper, Heal, PactOfDeath
}

public enum StatType
{
    Health, Mana, AttackPower, Defense, Speed, RunSpeed, LifeSteel,Money,Size
}

public enum StatModifyType
{
    Perment, Buff, Equipment, Damage
}

public enum TileType
{
    Empty,
    Floor,
    Wall,
    Pillar,
    Celling,
}

public enum StateType
{
    Idle, Walk, Attack, SwapWeapon, Skill
}

[BlackboardEnum]
public enum EnemyStateType
{
    Idle, Patrol, Chase, Attack
}


[BlackboardEnum]
public enum EnemyAttackType
{
    Closed, Ranged
}

public enum WeaponType
{
    None, Sword, Knife, Size
}

public enum InputSkill
{
    SkillQ, SkillW, SkillE, Size
}

public enum ItemType
{
    None, Helmet, Shoulder, Armor, Weapon, Boots, Pants
}

public enum UIState
{
    None, Inventory, SkillTree, 
}

public enum InputActionMap
{
    Player,UI
}

public enum CSVFIledType
{
    None, StatArr,
}

public enum TargetTag
{
    None, Player, Enemy
}

public static class TagManager
{
    private static Dictionary<TargetTag, string> m_TagToString = new()
    {
        { TargetTag.Player, "Player" },
        { TargetTag.Enemy, "Enemy" },
    };

    private static readonly Dictionary<string, TargetTag> m_StringToTag = m_TagToString.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string GetTagString(TargetTag tag)
    {
        if (m_TagToString.TryGetValue(tag, out string result)) { return result; }
        return "Untagged";
    }
    public static TargetTag TryGetTargetTag(string tag)
    {
        if (m_StringToTag.TryGetValue(tag, out TargetTag result)) { return result; }
        return TargetTag.None;
    }
}