using System.Collections.Generic;
using UnityEngine;



public class PlayerStateMachine : MonoBehaviour
{
    public StateType curType;

    private Dictionary<StateType, State> states;
    private State curState;
    public PlayerController ctrl;

    void Start()
    {
        states = new Dictionary<StateType, State>()
        {
            { StateType.Idle,new IdleState(ctrl) },
            { StateType.Walk,new WalkState(ctrl) },
            { StateType.Attack,new AttackState(ctrl) },
            { StateType.SwapWeapon,new SwapWeaponState(ctrl) },
            { StateType.Skill,new SkillState(ctrl) },
        };

        // 시작 상태 세팅
        curType = StateType.Idle;
        curState = states[curType];
        curState.Enter();
    }

    void Update()
    {
        curState?.Update();
    }

    public void ChangeState(StateType type)
    {
        //print($"{curType}에서 {type}으로 전환");
        curState?.Exit();
        curType = type;
        curState = states[curType];
        curState?.Enter();
    }

    public bool CanOtherAction()
    {
        bool canAction = curType == StateType.Idle 
                        || curType == StateType.Walk;
        return canAction;
    }
}
