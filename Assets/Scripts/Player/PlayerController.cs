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

    private Vector3 m_desiredPlanarVelocity;
    private Quaternion m_desiredRotation;
    private bool m_hasDesiredRotation;
    private Vector3 m_pendingRootMotion;

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

}
