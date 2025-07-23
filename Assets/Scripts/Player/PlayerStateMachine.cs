using System.Collections.Generic;
using UnityEngine;



public class PlayerStateMachine : MonoBehaviour
{
    [Header("Require Setting")]
    public PlayerController Ctrl;
    public StateType CurType;

    private Dictionary<StateType, State> states;
    private State curState;

    void Start()
    {
        states = new Dictionary<StateType, State>()
        {
            { StateType.Idle,new IdleState(Ctrl) },
            { StateType.Walk,new WalkState(Ctrl) },
            { StateType.Attack,new AttackState(Ctrl) },
            { StateType.SwapWeapon,new SwapWeaponState(Ctrl) },
            { StateType.Skill,new SkillState(Ctrl) },
        };

        // 시작 상태 세팅
        CurType = StateType.Idle;
        curState = states[CurType];
        curState.Enter();
    }

    void Update()
    {
        curState?.Update();
    }

    public void ChangeState(StateType type)
    {
        curState?.Exit();
        CurType = type;
        curState = states[CurType];
        curState?.Enter();
    }

    public bool CanOtherAction()
    {
        bool canAction = CurType == StateType.Idle 
                        || CurType == StateType.Walk;
        return canAction;
    }
}
