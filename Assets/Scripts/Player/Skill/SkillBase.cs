using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "SkillBase", menuName = "Scriptable Objects/Skill Base")]
public abstract class SkillBase : ScriptableObject
{
    private static int skillAnimationParam = Animator.StringToHash("SkillType");

    public string skillName = "어떤 스킬";
    public string description = "스킬 설명을 써주세요.";
    public Sprite icon;
    public VFX SkillType = VFX.None;
    public float activeDelay = 1f;
    public float effectDelay = 0.5f;
    public float cooldown = 5f;
    public float costMP = 10f;
    public bool isLocked = true;
    public WeaponType requireWeapon;
    public Vector3 startOffset = Vector3.zero;

    private PlayerController player;
    protected Vector3 center;
    protected Vector3 foward;
    protected Vector3 right;
    protected Vector3 up;

    protected bool isBreak;

    [HideInInspector] public float LastSkillUseTime;

    public virtual IEnumerator Active(PlayerController player)
    {
        LastSkillUseTime = Time.time;
        var mp = player.model.Stats[StatType.Mana];

        // 쿨타임 + mp 확인
        if (Time.realtimeSinceStartup - LastSkillUseTime < cooldown
            || costMP > mp.Value)
        {
            isBreak = true;
            yield break;
        }
        
        mp.Value -= costMP;
        LastSkillUseTime = Time.realtimeSinceStartup;

        this.player = player;
        var trans = player.transform;
        foward = trans.forward;
        right = trans.right;
        up = trans.up;
        center = trans.position + foward * startOffset.z + right * startOffset.x + up * startOffset.y;

        if (player.combat.curType != requireWeapon && requireWeapon != WeaponType.None)
        {
            Debug.Log("전용 무기 장착 후, 사용");
            yield break;
        }

        if (SkillType != VFX.None)
        {
            // 각 스킬에 따른 애니메이션 재생
            player.animator.SetInteger(skillAnimationParam, (int)SkillType);
            player.machine.ChangeState(StateType.Skill);

            // 각 스킬에 따른 이펙트 재생
            SkillEffectManager.Instance.PlayVFX(SkillType, center, player.transform.rotation,effectDelay);
        }
        yield return new WaitForSeconds(activeDelay);
    }

    public virtual bool TryUnlockSkill()
    {
        return false;
    }
}
