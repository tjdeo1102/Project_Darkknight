using UnityEngine;

public class IdleState : State
{
    public IdleState(PlayerController controller) : base(controller) { }

    public override void Update()
    {
        base.Update();

        var walkInput = ctrl.moveDir;

        if (ctrl.machine.CurType != StateType.Walk && 
            walkInput.sqrMagnitude > 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Walk);
        }
    }
}
