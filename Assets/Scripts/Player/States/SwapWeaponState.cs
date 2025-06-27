using UnityEngine;

public class SwapWeaponState : State
{
    private string tagName = "SwapWeapon";
    private int swapWeaponParam = Animator.StringToHash("SwapWeapon");
    private float exitMinTime = 0.25f;
    private float enterTime;

    public SwapWeaponState(PlayerController controller) : base(controller) {  }

    public override void Enter()
    {
        base.Enter();

        ctrl.animator.SetTrigger(swapWeaponParam);
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
