using UnityEngine;

public class IdleState : State
{
    public IdleState(PlayerController controller) : base(controller) { }

    public override void Update()
    {
        base.Update();

        var WalkInput = ctrl.input.actions["Move"].ReadValue<Vector2>();

        if (WalkInput.sqrMagnitude > 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Walk);
        }
    }
}
