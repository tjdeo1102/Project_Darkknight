using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerInput input;
    public PlayerStateMachine machine;
    public PlayerModel model;
    public PlayerCombat combat;
    public Animator animator;

    public CharacterController moveController;


}
