using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAttack", menuName = "Scriptable Objects/Weapon Attack")]
public class WeaponAttackSO : ScriptableObject
{
    [Header("Timing")]
    public float EffectDelay;
    public float ActiveDelay = 0.1f;
    public bool UseAnimationEvent = true;

    [Header("Hit")]
    public float AdditionalDamage;
    public float KnockBackForce = 1f;
    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;

    [Header("VFX")]
    public ParticleSystem MainVFXPrefab;
    public ParticleSystem HitVFXPrefab;
    public Vector3 MainVFXOffset = Vector3.zero;
    public Vector3 MainVFXEulerAngles = Vector3.zero;
    public Vector3 HitVFXOffset = Vector3.zero;
    public Vector3 HitVFXEulerAngles = Vector3.zero;

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

    public Vector3 GetHitVFXPosition(Transform origin, Vector3 hitPosition)
    {
        return hitPosition + origin.right * HitVFXOffset.x + origin.up * HitVFXOffset.y + origin.forward * HitVFXOffset.z;
    }

    public Quaternion GetHitVFXRotation(Transform origin)
    {
        return GetHitRotation(origin) * Quaternion.Euler(HitVFXEulerAngles);
    }

    private Vector3 GetLocalOffsetPosition(Transform origin, Vector3 offset)
    {
        return origin.position + origin.right * offset.x + origin.up * offset.y + origin.forward * offset.z;
    }
}
