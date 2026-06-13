using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;

public enum VFX
{
    None, VerticalSlash, HorizontalSlash, Smash, SpinSlash, FireBreath, SkyRipper, Heal, PactOfDeath, Boss1_ThrowSkill, Boss1_JumpSkill
}

public enum ProjectileType
{
    None, ClosedEnemyBullet, Boss1_ThrowBullet
}

public enum StatType
{
    Health, Mana, AttackPower, Defense, Speed, RunSpeed, LifeSteel,Money,Size
}

[System.Flags]
public enum StatModifyType
{
    None = 0,
    Permanent = 1 << 0,
    Buff = 1 << 1,
    Equipment = 1 << 2, 
    Damage = 1 << 3,
    SkillUse = 1 << 4,
    KillEnemy = 1 << 5,
    BuyItem = 1 << 6,
    DialogReward = 1 << 7,
}

public enum TileType
{
    Empty,
    Floor,
    Wall,
    Pillar,
    Celling,
    Gate,
    Size,
}

public enum StateType
{
    Idle, Walk, Attack, SwapWeapon, Skill
}

[System.Flags]
public enum EnemyType
{
    None = 0,
    NormalBlade = 1 << 0,   
    NormalGunner = 1 << 1,  
    CinemaBoss_1 = 1 << 2,              
    CinemaBoss_2 = 1 << 3,              
    CinemaBoss_3 = 1 << 4,
    Boss_1 = 1 << 5,        
    Boss_2 = 1 << 6,        
    Boss_3 = 1 << 7,
}

[System.Flags]
public enum NPCType
{
    None = 0,
    Npc1 = 1 << 0,
    Npc2 = 1 << 1, 
    Npc3 = 1 << 2, 
    Npc4 = 1 << 3 
}

[BlackboardEnum]
public enum EnemyStateType
{
    Idle, Patrol, Chase, Attack, KnockBack, Die
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
    None, RadialMenu ,Inventory, SkillTree, Dialog, Die, Setting, MidBossClear, GameClear,
}

public enum InputActionMap
{
    Player,UI,Dialog,Pause
}

public enum CSVFIledType
{
    None, StatArr,
}

public enum CSVImportType
{
    SkillBase, InventroyItem, StatBaseSO
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

public enum TargetLayer
{
    None, Player, Enemy
}

public static class TargetLayerManager
{
    private static Dictionary<TargetLayer, LayerMask> m_LayerToLayerMask = new()
    {
        { TargetLayer.None, LayerMask.GetMask("Default") },
        { TargetLayer.Player, LayerMask.GetMask("Player") },
        { TargetLayer.Enemy, LayerMask.GetMask("Enemy") },
    };

    public static LayerMask GetLayerMask(TargetLayer tag)
    {
        if (m_LayerToLayerMask.TryGetValue(tag, out LayerMask result)) { return result; }
        return LayerMask.GetMask("Default");
    }
}
