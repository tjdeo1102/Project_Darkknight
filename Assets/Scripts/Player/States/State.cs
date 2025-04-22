using UnityEngine;

public abstract class State
{
    protected Animator anim;
    protected PlayerController ctrl;

    public State(PlayerController controller)
    {
        this.ctrl = controller;
        anim = controller.animator;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
