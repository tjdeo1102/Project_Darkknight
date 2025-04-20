using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static UnityEditor.Rendering.InspectorCurveEditor;
using static UnityEditor.VersionControl.Asset;

public enum StateType
{
    Idle,Walk
}

public class PlayerStateMachine : MonoBehaviour
{
    private StateType curType;

    private Dictionary<StateType, State> states;
    private State curState;
    public Animator animator;
    public PlayerController controller;

    void Start()
    {
        states = new Dictionary<StateType, State>()
        {
            { StateType.Idle,new IdleState(controller, animator) },
            { StateType.Walk,new WalkState(controller, animator) }
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
        curState?.Exit();
        curType = type;
        curState = states[curType];
        curState?.Enter();
    }

}
