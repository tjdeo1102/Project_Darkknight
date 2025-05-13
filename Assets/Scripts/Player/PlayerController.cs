using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerInput input;
    public PlayerStateMachine machine;
    public PlayerModel model;
    public PlayerCombat combat;
    public Animator animator;
    public Transform body;

    public CharacterController moveController;

    private void LateUpdate()
    {
        var pos = body.localPosition;
        transform.position += transform.TransformDirection(pos);
        body.localPosition = Vector3.zero;
    }
}
