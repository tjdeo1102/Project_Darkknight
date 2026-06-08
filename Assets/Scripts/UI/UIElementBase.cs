using UnityEngine;

public class UIElementBase : MonoBehaviour
{
    public UIController Controller;
    public UIState Type;
    public bool DependencyOnChangeState = true;

    protected virtual void OnDisable()
    {
        if (Controller != null && Controller.CurrentState == Type)
            Controller.ChangeState(UIState.None);
    }
}
