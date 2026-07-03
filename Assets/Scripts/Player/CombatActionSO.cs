using UnityEngine;
using System.Collections;

public abstract class CombatActionSO : CSVScriptableObject
{
    [Header("Animation")]
    [Tooltip("Animator state path to play for this action. Example: Base Layer.Attack.SwordAttack1. Leave empty to use the legacy parameter-driven transition.")]
    public string AnimatorStateName;
    [Min(0)] public int AnimatorLayerIndex;
    [Min(0f)] public float CrossFadeDuration = 0.05f;
    [Range(0f, 1f), Tooltip("다음 연계 입력이 있을 때, 현재 공격을 캔슬하고 넘어갈 수 있는 최소 시점 (기존 exitMinTime 대체)")]
    public float CancelAvailableNormalizedTime = 0.25f;

    [Range(0f, 1f), Tooltip("다음 연계 입력이 없을 때, 자연스럽게 Idle로 돌아가는 시점")]
    public float FinishNormalizedTime = 0.75f;

    [Header("Hit Feedback")]
    public bool EnableHitStop = true;
    [Min(0f)] public float HitStopDuration = 0.04f;
    [Range(0.01f, 1f)] public float HitStopTimeScale = 0.05f;
    public bool EnableCameraImpulse;
    [Min(0f)] public float CameraImpulseForce = 0.2f;

    [System.NonSerialized] private bool m_hasWarnedInvalidAnimatorState;
    [System.NonSerialized] private string m_cachedAnimatorStateName;
    [System.NonSerialized] private int m_cachedAnimatorLayerIndex = -1;
    [System.NonSerialized] private int m_cachedAnimatorStateHash;

    public bool HasExplicitAnimatorState => string.IsNullOrWhiteSpace(AnimatorStateName) == false;

    public bool TryPlayAnimatorState(Animator animator)
    {
        if (animator == null || HasExplicitAnimatorState == false)
            return false;

        var layerIndex = Mathf.Clamp(AnimatorLayerIndex, 0, animator.layerCount - 1);
        if (TryGetStateHash(animator, layerIndex, out var stateHash) == false)
        {
            WarnInvalidAnimatorState(animator, layerIndex);
            return false;
        }

        animator.CrossFadeInFixedTime(stateHash, CrossFadeDuration, layerIndex);
        return true;
    }

    public bool TryGetStateHash(Animator animator, int layerIndex, out int stateHash)
    {
        stateHash = 0;
        if (animator == null || HasExplicitAnimatorState == false)
            return false;

        layerIndex = Mathf.Clamp(layerIndex, 0, animator.layerCount - 1);
        var trimmedStateName = AnimatorStateName.Trim();
        if (m_cachedAnimatorLayerIndex == layerIndex &&
            m_cachedAnimatorStateName == trimmedStateName &&
            m_cachedAnimatorStateHash != 0)
        {
            if (animator.HasState(layerIndex, m_cachedAnimatorStateHash))
            {
                stateHash = m_cachedAnimatorStateHash;
                return true;
            }

            ClearAnimatorStateCache();
        }

        var layerName = animator.GetLayerName(layerIndex);
        if (TryResolveStateHash(animator, layerIndex, trimmedStateName, out stateHash) ||
            TryResolveStateHash(animator, layerIndex, $"{layerName}.{trimmedStateName}", out stateHash))
            return true;

        return false;
    }

    private bool TryResolveStateHash(Animator animator, int layerIndex, string stateName, out int stateHash)
    {
        stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(layerIndex, stateHash) == false)
        {
            stateHash = 0;
            return false;
        }

        m_cachedAnimatorLayerIndex = layerIndex;
        m_cachedAnimatorStateName = AnimatorStateName.Trim();
        m_cachedAnimatorStateHash = stateHash;
        return true;
    }

    private void WarnInvalidAnimatorState(Animator animator, int layerIndex)
    {
        if (m_hasWarnedInvalidAnimatorState || animator == null)
            return;

        m_hasWarnedInvalidAnimatorState = true;
        Debug.LogWarning(
            $"{name} has invalid AnimatorStateName '{AnimatorStateName}' on layer {layerIndex}. Falling back to the legacy animation trigger.",
            this);
    }

    private void ClearAnimatorStateCache()
    {
        m_cachedAnimatorLayerIndex = -1;
        m_cachedAnimatorStateName = null;
        m_cachedAnimatorStateHash = 0;
    }
}

public static class CombatActionFeedback
{
    private static Coroutine s_hitStopRoutine;
    private static float s_restoreTimeScale = 1f;
    private static float s_restoreFixedDeltaTime = 0.02f;

    public static void PlayOnHit(CombatActionSO action, Component owner)
    {
        if (action == null || owner == null) return;

        var player = owner.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (action.EnableCameraImpulse && player.Impulse != null && action.CameraImpulseForce > 0f)
            player.Impulse.GenerateImpulse(action.CameraImpulseForce);

        if (action.EnableHitStop)
            StartHitStop(player, action.HitStopDuration, action.HitStopTimeScale);
    }

    private static void StartHitStop(MonoBehaviour runner, float duration, float timeScale)
    {
        if (runner == null || runner.isActiveAndEnabled == false) return;
        if (duration <= 0f || Time.timeScale <= 0f) return;

        if (s_hitStopRoutine != null)
        {
            runner.StopCoroutine(s_hitStopRoutine);
            RestoreTimeScale();
        }

        s_restoreTimeScale = Time.timeScale;
        s_restoreFixedDeltaTime = Time.fixedDeltaTime;
        s_hitStopRoutine = runner.StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    private static IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        Time.timeScale = Mathf.Clamp(timeScale, 0.01f, 1f);
        Time.fixedDeltaTime = s_restoreFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        RestoreTimeScale();
        s_hitStopRoutine = null;
    }

    private static void RestoreTimeScale()
    {
        Time.timeScale = s_restoreTimeScale;
        Time.fixedDeltaTime = s_restoreFixedDeltaTime;
    }
}
