using UnityEngine;

public static class CombatActionRunner
{
    public static bool TryPlayAnimation(CombatActionSO action, Animator animator)
    {
        return action != null && action.TryPlayAnimatorState(animator);
    }
}
