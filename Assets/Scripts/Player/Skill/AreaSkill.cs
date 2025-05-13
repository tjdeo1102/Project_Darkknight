using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaSkill", menuName = "Scriptable Objects/Area Skill")]
public class AreaSkill: SkillBase
{
    public Vector3 hitOffset = Vector3.zero;
    public Vector3 range = Vector3.zero;
    public float damage = 50f;
    public LayerMask targetLayer;

    public override IEnumerator Active(PlayerController player)
    {
        yield return base.Active(player);

        if (isBreak)
        {
            isBreak = false;
            yield break;
        }

        if (player.combat.curType != requireWeapon) yield break;

        var center = this.center + foward * hitOffset.z + right * hitOffset.x + up * hitOffset.y;
        Vector3 halfExtents = range * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, targetLayer);

        Tool.DrawOverlapBox(center, range, Quaternion.LookRotation(foward), Color.green, 2f);

        foreach (Collider hit in hits)
        {
            // 피격 적용 로직
            Debug.Log("피격됨");
        }
        yield break;
    }


}
