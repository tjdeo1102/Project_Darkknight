using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaSkill", menuName = "Scriptable Objects/Area Skill")]
public class AreaSkill: SkillBase
{
    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;
    [CSVField(CSVFIledType.None)]
    public float Damage = 50f;
    [CSVField(CSVFIledType.None)]
    public float DamageFactor = 0f;
    public LayerMask TargetLayer;

    public override IEnumerator Active(PlayerController player)
    {
        yield return base.Active(player);

        if (isFailSkill) yield break;

        if (player.combat.CurType != RequireWeapon) yield break;

        var center = this.center + foward * HitOffset.z + right * HitOffset.x + up * HitOffset.y;
        Vector3 halfExtents = Range * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, TargetLayer);

        Tool.DrawOverlapBox(center, Range, Quaternion.LookRotation(foward), Color.green, 2f);

        foreach (Collider hit in hits)
        {
            // 피격 적용 로직
            Debug.Log("피격됨");
        }
        yield break;
    }


}
