using UnityEngine;

public class UIElementBase : MonoBehaviour
{
    public UIController Controller;
    public UIState Type;
    public bool DependencyOnChangeState = true;

    protected virtual void OnDisable()
    {
    }
}
