using UnityEngine;

public class WalkState : State
{
    private readonly int WalkParam = Animator.StringToHash("Walk");
    private readonly int RunParam = Animator.StringToHash("Run");
    public WalkState(PlayerController controller) : base(controller) { }

    public override void Update()
    {
        base.Update();
        var input = ctrl.input.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        float runInput = ctrl.input.actions["Run"].ReadValue<float>();
            
        if (move.sqrMagnitude < 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }

        ctrl.transform.rotation = Quaternion.LookRotation(move);
        if (runInput > 0.1f)
        {
            anim.SetBool(RunParam, true);
            anim.SetBool(WalkParam, false);
            ctrl.moveController.Move(ctrl.model.RunSpeed.Value * Time.deltaTime * ctrl.transform.forward);
        }
        else
        {
            anim.SetBool(RunParam, false);
            anim.SetBool(WalkParam, true);
            ctrl.moveController.Move(ctrl.model.Speed.Value * Time.deltaTime * ctrl.transform.forward);
        }
    }

    public override void Exit() 
    { 
        base.Exit();
        anim.SetBool(WalkParam, false);
        anim.SetBool(RunParam, false);
    }
}
