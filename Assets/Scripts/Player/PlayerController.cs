using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerController : MonoBehaviour
{
    [Header("Require Setting")]
    public PlayerInput input;
    public PlayerStateMachine machine;
    public PlayerModel model;
    public PlayerCombat combat;
    public Animator animator;
    public Transform body;
    public Rigidbody Rigid;
    public CinemachineImpulseSource Impulse;
    public Volume hitScreenVolume;

    private void LateUpdate()
    {
        var pos = body.localPosition;
        transform.position += transform.TransformDirection(pos);
        body.localPosition = Vector3.zero;
    }

    public void SetActionMap(InputActionMap mode)
    {
        if (input == null || input.actions == null) return;

        if (input.enabled == false)
            input.enabled = true;

        var mapName = mode.ToString();
        if (input.actions.FindActionMap(mapName, false) == null)
        {
            Debug.LogWarning($"Input action map not found: {mapName}");
            return;
        }

        if (input.currentActionMap != null && input.currentActionMap.name == mapName) return;

        input.SwitchCurrentActionMap(mapName);
    }

}
