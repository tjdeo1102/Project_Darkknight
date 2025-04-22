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

        ctrl.animator.SetTrigger(attackParam);
        enterTime = Time.time;

    }

    public override void Update()
    {
        base.Update();
        if (Time.time - enterTime < exitMinTime) return;
        if (!ctrl.animator.GetCurrentAnimatorStateInfo(0).IsTag(tagName))
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
    }
}
