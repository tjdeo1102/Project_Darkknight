using UnityEngine;

public class AttackState : State
{
    private readonly string tagName = "Attack";
    private readonly int attackParam = Animator.StringToHash("Attack");
    private readonly float exitMinTime = 0.25f;
    private float enterTime;
    public AttackState(PlayerController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        var weapon = ctrl.combat != null ? ctrl.combat.GetCurWeapon() : null;
        if (weapon == null || weapon.TryPlayCurrentAttackAnimation() == false)
            ctrl.animator.SetTrigger(attackParam);
        enterTime = Time.time;

    }

    public override void Update()
    {
        base.Update();
        if (Time.time - enterTime < exitMinTime) return;

        var stateInfo = ctrl.animator.GetCurrentAnimatorStateInfo(0);
        bool inTransition = ctrl.animator.IsInTransition(0);

        if (!inTransition && !stateInfo.IsTag(tagName))
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
    }
}
