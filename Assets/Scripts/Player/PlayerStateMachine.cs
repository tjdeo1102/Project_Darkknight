using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static UnityEditor.Rendering.InspectorCurveEditor;
using static UnityEditor.VersionControl.Asset;

public enum StateType
{
    Idle,Walk,Attack,SwapWeapon
}

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

}
