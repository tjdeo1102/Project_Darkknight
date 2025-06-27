using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Projectile Skill")]
public class ProjectileSkill: SkillBase
{
    public GameObject BulletPref;
    public float Speed;
    public float Damage;

    //public override void Active()
    //{
    //    base.Active();
    //}
}
