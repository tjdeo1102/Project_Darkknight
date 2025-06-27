using UnityEngine;

public class WalkState : State
{
    private static readonly string moveAction = "Move";
    private static readonly string runAction = "Run";
    private readonly int walkParam = Animator.StringToHash("Walk");
    private readonly int runParam = Animator.StringToHash("Run");

    private Transform m_cam;

    public WalkState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        m_cam = Camera.main.transform;
    }

    public override void Update()
    {
        base.Update();
        var input = ctrl.input.actions[moveAction].ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        float runInput = ctrl.input.actions[runAction].ReadValue<float>();
            
        if (move.sqrMagnitude < 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
        var camDir = new Vector3(m_cam.forward.x, 0, m_cam.forward.z);
        var dir = Quaternion.LookRotation(camDir) * move;
        ctrl.transform.rotation = Quaternion.LookRotation(dir);
        if (runInput > 0.1f)
        {
            anim.SetBool(runParam, true);
            anim.SetBool(walkParam, false);
            ctrl.rigid.linearVelocity = ctrl.model.RunSpeed.TotalValue * new Vector3(dir.x, 0, dir.z);
        }
        else
        {
            anim.SetBool(runParam, false);
            anim.SetBool(walkParam, true);
            ctrl.rigid.linearVelocity = ctrl.model.Speed.TotalValue * new Vector3(dir.x, 0, dir.z);
        }
    }

    public override void Exit() 
    { 
        base.Exit();
        anim.SetBool(walkParam, false);
        anim.SetBool(runParam, false);
        ctrl.rigid.linearVelocity = new Vector3(0, ctrl.rigid.linearVelocity.y, 0);
    }
}
