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

public enum WeaponType
{
    None, Sword, Knife, Gun, Size
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