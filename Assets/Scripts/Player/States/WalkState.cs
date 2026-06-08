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
        if (m_cam == null)
            m_cam = Camera.main.transform;
    }

    public override void Update()
    {
        base.Update();
        var input = ctrl.input.actions[moveAction].ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y).normalized;
            
        if (move.sqrMagnitude < 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
        var camDir = new Vector3(m_cam.forward.x, 0, m_cam.forward.z);
        var dir = Quaternion.LookRotation(camDir) * move;
        ctrl.transform.rotation = Quaternion.LookRotation(dir);

        float runInput = ctrl.input.actions[runAction].ReadValue<float>();
        var isRun = runInput > 0.1f;
        var spd =  isRun ? ctrl.model.RunSpeed.TotalValue : ctrl.model.Speed.TotalValue;
        
        anim.SetBool(runParam, isRun);
        anim.SetBool(walkParam, !isRun);

        ctrl.Rigid.linearVelocity = new Vector3(dir.x * spd, ctrl.Rigid.linearVelocity.y, dir.z * spd);
    }

    public override void Exit() 
    { 
        base.Exit();
        anim.SetBool(walkParam, false);
        anim.SetBool(runParam, false);
        ctrl.Rigid.linearVelocity = new Vector3(0, ctrl.Rigid.linearVelocity.y, 0);
    }
}
