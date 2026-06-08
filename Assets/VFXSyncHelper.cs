using System.Collections;
using UnityEngine;

public class VFXSyncHelper : MonoBehaviour
{
    public PlayerCombat PlayerCombat;

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

        weapon.ApplyAnimationEventHit();

        if (action.MainVFXPrefab != null || action.HitVFXPrefab != null)
            PlayAttackActionVFX(action, weapon.transform);
    }

    private void PlayAttackActionVFX(WeaponAttackSO action, Transform weaponTransform)
    {
        if (action.MainVFXPrefab != null)
            PlayPrefabVFX(action.MainVFXPrefab, action.GetMainVFXPosition(weaponTransform), action.GetMainVFXRotation(weaponTransform));

        if (action.HitVFXPrefab != null && PlayerCombat.GetCurWeapon().TryGetLastHitPosition(out var hitPosition))
            PlayPrefabVFX(action.HitVFXPrefab, action.GetHitVFXPosition(weaponTransform, hitPosition), action.GetHitVFXRotation(weaponTransform));
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
