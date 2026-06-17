using UnityEngine;

public class UIElementBase : MonoBehaviour
{
    public UIController Controller;
    public UIState Type;
    public bool DependencyOnChangeState = true;

    protected virtual void OnDisable()
    {
        if (Controller != null &&
            Controller.IsChangingState == false &&
            Controller.CurrentState == Type)
        {
            if (Controller.HasOverlayReturn)
                Controller.CloseOverlay();
            else
                Controller.ChangeState(UIState.None);
        }
    }
}
