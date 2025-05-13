using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Projectile Skill")]
public class ProjectileSkill: SkillBase
{
    public GameObject bulletPref;
    public float speed;
    public float damage;

    //public override void Active()
    //{
    //    base.Active();
    //}
}
