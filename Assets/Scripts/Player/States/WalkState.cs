using UnityEngine;

public class WalkState : State
{
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
        var input = ctrl.moveDir;
        Vector3 move = new Vector3(input.x, 0, input.y).normalized;
            
        if (move.sqrMagnitude < 0.1f)
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
        var camDir = new Vector3(m_cam.forward.x, 0, m_cam.forward.z);
        var dir = Quaternion.LookRotation(camDir) * move;

        float runInput = ctrl.runInput;
        var isRun = runInput > 0.1f;
        var spd =  isRun ? ctrl.model.RunSpeed.TotalValue : ctrl.model.Speed.TotalValue;
        
        anim.SetBool(runParam, isRun);
        anim.SetBool(walkParam, !isRun);

        var targetRot = Quaternion.LookRotation(dir);
        var smoothRot = Quaternion.Slerp(ctrl.transform.rotation, targetRot, 12f * Time.deltaTime);
        ctrl.SetMovement(dir * spd, smoothRot);
    }

    public override void Exit() 
    { 
        base.Exit();
        anim.SetBool(walkParam, false);
        anim.SetBool(runParam, false);
        ctrl.StopMovement();
    }
}
