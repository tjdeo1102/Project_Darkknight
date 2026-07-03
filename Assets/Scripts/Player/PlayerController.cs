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
    
    public Vector3 moveDir;
    public float runInput;

    private Vector3 m_desiredPlanarVelocity;
    private Quaternion m_desiredRotation;
    private bool m_hasDesiredRotation;
    private Vector3 m_pendingRootMotion;

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void LateUpdate()
    {
        if (body == null) return;

        m_pendingRootMotion += transform.TransformDirection(body.localPosition);
        body.localPosition = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (Rigid == null) return;

        if (Rigid.isKinematic == false)
        {
            var velocity = Rigid.linearVelocity;
            Rigid.linearVelocity = new Vector3(
                m_desiredPlanarVelocity.x,
                velocity.y,
                m_desiredPlanarVelocity.z);
        }

        if (m_hasDesiredRotation)
        {
            Rigid.MoveRotation(m_desiredRotation);
        }

        if (m_pendingRootMotion.sqrMagnitude > Mathf.Epsilon)
        {
            Rigid.MovePosition(Rigid.position + m_pendingRootMotion);
            m_pendingRootMotion = Vector3.zero;
        }
    }

    public void SetMovement(Vector3 planarVelocity, Quaternion rotation)
    {
        m_desiredPlanarVelocity = planarVelocity;
        m_desiredRotation = rotation;
        m_hasDesiredRotation = true;
    }

    public void StopMovement()
    {
        m_desiredPlanarVelocity = Vector3.zero;
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

    #region Input Handling
    private bool m_isInputSubscribed;
    private InputAction m_walkAction;
    private InputAction m_runAction;
    private InputAction m_skillAction;
    private InputAction m_swapWeaponAction;
    private InputAction m_attackAction;

    private void SubscribeInput()
    {
        if (m_isInputSubscribed) return;
        if (input == null || input.actions == null) return;

        m_walkAction = input.actions.FindAction("Player/Move");
        m_runAction = input.actions.FindAction("Player/Run");
        m_skillAction = input.actions.FindAction("Player/Skill");
        m_swapWeaponAction = input.actions.FindAction("Player/SwapWeapon");
        m_attackAction = input.actions.FindAction("Player/Attack");
        if (m_skillAction == null || m_swapWeaponAction == null || m_attackAction == null) return;

        m_walkAction.performed += OnWalk;
        m_walkAction.canceled += OnWalk;
        m_runAction.performed += OnRun;
        m_runAction.canceled += OnRun;
        m_skillAction.performed += combat.OnSkill;
        m_swapWeaponAction.performed += combat.OnSwapWeapon;
        m_attackAction.performed += combat.OnAttack;
        m_isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (m_isInputSubscribed == false) return;
        if (input == null || input.actions == null)
        {
            m_isInputSubscribed = false;
            return;
        }

        m_skillAction.performed -= combat.OnSkill;
        m_swapWeaponAction.performed -= combat.OnSwapWeapon;
        m_attackAction.performed -= combat.OnAttack;
        m_walkAction.performed -= OnWalk;
        m_walkAction.canceled -= OnWalk;
        m_runAction.performed -= OnRun;
        m_runAction.canceled -= OnRun;
        m_isInputSubscribed = false;
    }


    public void OnWalk(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        runInput = context.ReadValue<float>();
    }

    #endregion
}
