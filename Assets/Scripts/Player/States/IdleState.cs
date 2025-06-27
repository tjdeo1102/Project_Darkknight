using UnityEngine;

public class IdleState : State
{
    private static readonly string moveAction = "Move";

    public IdleState(PlayerController controller) : base(controller) { }

    public override void Update()
    {
        base.Update();

        var walkInput = ctrl.input.actions[moveAction].ReadValue<Vector2>();

        if (ctrl.machine.CurType != StateType.Walk && 
            walkInput.sqrMagnitude > 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Walk);
        }
    }
}
