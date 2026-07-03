using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAttack", menuName = "Scriptable Objects/Weapon Attack")]
public class WeaponAttackSO : CombatActionSO
{
    [Header("Timing")]
    [Tooltip("Delay after an animation event before resolving this attack. Used immediately when UseAnimationEvent is disabled.")]
    public float EffectDelay;
    [Tooltip("Delay after PlayVFX animation event before hit resolution. If animation events are disabled, this is used from attack start.")]
    public float ActiveDelay = 0.1f;
    [Tooltip("When UseAnimationEvent is enabled, this attack will still resolve after this many seconds if the animation event is missing. Set to 0 or less to disable the fallback.")]
    public float AnimationEventFallbackDelay = 0.6f;
    [Tooltip("Prefer animation events for exact hit timing. The fallback delay protects attacks from being skipped when an event is missing.")]
    public bool UseAnimationEvent = true;

    [Header("Hit")]
    public float AdditionalDamage;
    public float KnockBackForce = 1f;
    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;

    [Header("Gizmos")]
    public bool ShowHitboxGizmo = true;
    public bool ShowMainVFXGizmo = true;

    [Header("VFX")]
    public ParticleSystem MainVFXPrefab;
    public ParticleSystem HitVFXPrefab;
    public Vector3 MainVFXOffset = Vector3.zero;
    public Vector3 MainVFXEulerAngles = Vector3.zero;
    public Vector3 MainVFXScale = Vector3.one;
    public Vector3 HitVFXOffset = Vector3.zero;
    public Vector3 HitVFXEulerAngles = Vector3.zero;
    public Vector3 HitVFXScale = Vector3.one;

    public Vector3 GetHitCenter(Transform origin)
    {
        return GetLocalOffsetPosition(origin, HitOffset);
    }

    public Quaternion GetHitRotation(Transform origin)
    {
        return Quaternion.LookRotation(origin.forward, origin.up);
    }

    public Vector3 GetMainVFXPosition(Transform origin)
    {
        return GetLocalOffsetPosition(origin, MainVFXOffset);
    }

    public Quaternion GetMainVFXRotation(Transform origin)
    {
        return GetHitRotation(origin) * Quaternion.Euler(MainVFXEulerAngles);
    }

    public Vector3 GetMainVFXScale()
    {
        return MainVFXScale;
    }

    public Vector3 GetHitVFXPosition(Transform origin, Vector3 hitPosition)
    {
        return hitPosition + origin.right * HitVFXOffset.x + origin.up * HitVFXOffset.y + origin.forward * HitVFXOffset.z;
    }

    public Quaternion GetHitVFXRotation(Transform origin)
    {
        return GetHitRotation(origin) * Quaternion.Euler(HitVFXEulerAngles);
    }

    public Vector3 GetHitVFXScale()
    {
        return HitVFXScale;
    }

#if UNITY_EDITOR
    [ContextMenu("VFX/Fit Main VFX To Hitbox")]
    private void FitMainVFXToHitbox()
    {
        WeaponAttackSOVFXFitter.FitMainVFXToHitbox(this);
    }

    [ContextMenu("VFX/Fit Hit VFX To Hitbox")]
    private void FitHitVFXToHitbox()
    {
        WeaponAttackSOVFXFitter.FitHitVFXToHitbox(this);
    }

    [ContextMenu("VFX/Copy Hitbox Center To Main VFX")]
    private void CopyHitboxCenterToMainVFX()
    {
        WeaponAttackSOVFXFitter.CopyHitboxCenterToMainVFX(this);
    }

    [ContextMenu("VFX/Reset Hit VFX Offset To Impact Point")]
    private void ResetHitVFXOffsetToImpactPoint()
    {
        WeaponAttackSOVFXFitter.ResetHitVFXOffsetToImpactPoint(this);
    }

    [ContextMenu("VFX/Reset VFX Transform Values")]
    private void ResetVFXTransformValues()
    {
        WeaponAttackSOVFXFitter.ResetVFXTransformValues(this);
    }

    [ContextMenu("VFX/Create Runtime Main VFX Preview")]
    private void CreateRuntimeMainVFXPreview()
    {
        WeaponAttackSOVFXFitter.CreateRuntimeMainVFXPreview(this);
    }

    [ContextMenu("VFX/Create Runtime Hit VFX Preview")]
    private void CreateRuntimeHitVFXPreview()
    {
        WeaponAttackSOVFXFitter.CreateRuntimeHitVFXPreview(this);
    }
#endif

    private Vector3 GetLocalOffsetPosition(Transform origin, Vector3 offset)
    {
        return origin.position + origin.right * offset.x + origin.up * offset.y + origin.forward * offset.z;
    }
}

public class WeaponAttackVFXPreview : MonoBehaviour
{
    [SerializeField] private WeaponAttackSO action;
    [SerializeField] private bool isMainVFX = true;

    public void Init(WeaponAttackSO sourceAction, bool previewMainVFX)
    {
        action = sourceAction;
        isMainVFX = previewMainVFX;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Preview Transform To SO")]
    private void ApplyPreviewTransformToSO()
    {
        if (action == null)
        {
            Debug.LogWarning("Preview has no WeaponAttackSO source.", this);
            return;
        }

        WeaponAttackSOVFXFitter.CopyPreviewTransformToSO(this, action, isMainVFX);
    }
#endif
}
