
using UnityEngine;

public class SkillState : State
{
    private readonly string tagName = "Skill";
    private readonly int skillParam = Animator.StringToHash("Skill");
    private readonly float exitMinTime = 0.25f;
    private float enterTime;

    public SkillState(PlayerController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        ctrl.animator.SetTrigger(skillParam);
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
