using System.Collections;
using UnityEngine;

public class VFXSyncHelper : MonoBehaviour
{
    public PlayerCombat PlayerCombat;
    private WeaponBase m_lastScheduledWeapon;
    private int m_lastScheduledAttackSequence = -1;

    private void Start()
    {
        if (InGameLoop.Instance == null || InGameLoop.Instance.Player == null)
        {
            PlayerCombat = FindAnyObjectByType<PlayerCombat>();
        }
        else PlayerCombat = InGameLoop.Instance.Player.combat;
    }

    public void PlayVFX()
    {
        if (PlayerCombat == null) return;
        var weapon = PlayerCombat.GetCurWeapon();
        if (weapon == null) return;

        var action = weapon.GetCurrentAttackAction();
        if (action == null) return;

        var attackSequence = weapon.GetCurrentAttackSequence();
        if (weapon == m_lastScheduledWeapon &&
            attackSequence == m_lastScheduledAttackSequence)
        {
            return;
        }

        m_lastScheduledWeapon = weapon;
        m_lastScheduledAttackSequence = attackSequence;
        StartCoroutine(PlayAttackActionVFX(weapon, action, attackSequence));
    }

    private IEnumerator PlayAttackActionVFX(
        WeaponBase weapon,
        WeaponAttackSO action,
        int attackSequence)
    {
        var weaponTransform = weapon.transform;
        var hitDelay = Mathf.Max(0f, action.ActiveDelay);
        var effectDelay = Mathf.Max(0f, action.EffectDelay);
        var elapsed = 0f;
        var hitResolved = false;
        var effectResolved = false;

        while (hitResolved == false || effectResolved == false)
        {
            if (weapon.IsAttackSequenceCurrent(action, attackSequence) == false)
                yield break;

            if (effectResolved == false && elapsed >= effectDelay)
            {
                effectResolved = true;
                if (action.MainVFXPrefab != null)
                {
                    PlayPrefabVFX(
                        action.MainVFXPrefab,
                        action.GetMainVFXPosition(weaponTransform),
                        action.GetMainVFXRotation(weaponTransform));
                }
            }

            if (hitResolved == false && elapsed >= hitDelay)
            {
                hitResolved = true;
                var hitSucceeded = weapon.ApplyAnimationEventHit(action, attackSequence);
                if (hitSucceeded &&
                    action.HitVFXPrefab != null &&
                    weapon.TryGetLastHitPosition(out var hitPosition))
                {
                    PlayPrefabVFX(
                        action.HitVFXPrefab,
                        action.GetHitVFXPosition(weaponTransform, hitPosition),
                        action.GetHitVFXRotation(weaponTransform));
                }
            }

            if (hitResolved && effectResolved)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PlayPrefabVFX(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        var instance = Instantiate(prefab, position, rotation);
        instance.Play();
        StartCoroutine(DestroyVFXAfterPlay(instance));
    }

    private IEnumerator DestroyVFXAfterPlay(ParticleSystem particle)
    {
        yield return new WaitForSeconds(particle.main.duration + particle.main.startLifetime.constantMax + 0.2f);
        if (particle != null)
            Destroy(particle.gameObject);
    }
}
