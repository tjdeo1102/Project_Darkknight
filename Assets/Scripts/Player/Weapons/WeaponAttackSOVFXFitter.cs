#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WeaponAttackSOVFXFitter
{
    private const float MinAxisSize = 0.01f;
    private const float DefaultFitPadding = 0.9f;
    private const float MinScaleMultiplier = 0.05f;
    private const float MaxScaleMultiplier = 5f;
    private const string RuntimePreviewName = "Runtime VFX Preview";

    public static void CopyHitboxCenterToMainVFX(WeaponAttackSO action)
    {
        ApplyChange(action, () => action.MainVFXOffset = action.HitOffset);
    }

    public static void ResetHitVFXOffsetToImpactPoint(WeaponAttackSO action)
    {
        ApplyChange(action, () => action.HitVFXOffset = Vector3.zero);
    }

    public static void ResetVFXTransformValues(WeaponAttackSO action)
    {
        ApplyChange(action, () =>
        {
            action.MainVFXOffset = Vector3.zero;
            action.MainVFXEulerAngles = Vector3.zero;
            action.MainVFXScale = Vector3.one;
            action.HitVFXOffset = Vector3.zero;
            action.HitVFXEulerAngles = Vector3.zero;
            action.HitVFXScale = Vector3.one;
        });
    }

    public static void FitMainVFXToHitbox(WeaponAttackSO action)
    {
        if (TryCalculateScale(action, action.MainVFXPrefab, out var scale) == false)
            return;

        ApplyChange(action, () =>
        {
            action.MainVFXOffset = action.HitOffset;
            action.MainVFXScale = scale;
        });
    }

    public static void FitHitVFXToHitbox(WeaponAttackSO action)
    {
        if (TryCalculateScale(action, action.HitVFXPrefab, out var scale) == false)
            return;

        ApplyChange(action, () =>
        {
            action.HitVFXOffset = Vector3.zero;
            action.HitVFXScale = scale;
        });
    }

    public static void CreateRuntimeMainVFXPreview(WeaponAttackSO action)
    {
        CreateRuntimeVFXPreview(action, isMainVFX: true);
    }

    public static void CreateRuntimeHitVFXPreview(WeaponAttackSO action)
    {
        CreateRuntimeVFXPreview(action, isMainVFX: false);
    }

    public static void CopyPreviewTransformToSO(
        WeaponAttackVFXPreview preview,
        WeaponAttackSO action,
        bool isMainVFX)
    {
        if (preview == null || action == null)
            return;

        var source = preview.transform;
        if (source.parent == null)
        {
            Debug.LogWarning("Preview must stay under a weapon transform to copy local VFX values.", preview);
            return;
        }

        ApplyChange(action, () =>
        {
            if (isMainVFX)
            {
                action.MainVFXOffset = source.localPosition;
                action.MainVFXEulerAngles = NormalizeEuler(source.localEulerAngles);
                action.MainVFXScale = DivideScale(source.localScale, GetPrefabScale(action.MainVFXPrefab));
            }
            else
            {
                action.HitVFXOffset = source.localPosition - action.HitOffset;
                action.HitVFXEulerAngles = NormalizeEuler(source.localEulerAngles);
                action.HitVFXScale = DivideScale(source.localScale, GetPrefabScale(action.HitVFXPrefab));
            }
        });

        Debug.Log($"Applied {preview.name} transform to {action.name}.", action);
    }

    private static void CreateRuntimeVFXPreview(WeaponAttackSO action, bool isMainVFX)
    {
        if (Application.isPlaying == false)
        {
            Debug.LogWarning("Runtime VFX preview can only be created while the scene is playing.", action);
            return;
        }

        var prefab = isMainVFX ? action.MainVFXPrefab : action.HitVFXPrefab;
        if (action == null || prefab == null)
        {
            Debug.LogWarning("VFX prefab is empty.", action);
            return;
        }

        if (TryFindRuntimeWeapon(action, out var weapon) == false)
        {
            Debug.LogWarning($"Could not find a runtime weapon using {action.name}.", action);
            return;
        }

        var instance = Object.Instantiate(prefab.gameObject, weapon.transform);
        instance.name = $"{RuntimePreviewName} - {action.name} - {(isMainVFX ? "Main" : "Hit")}";
        instance.hideFlags = HideFlags.DontSave;
        instance.transform.localPosition = isMainVFX
            ? action.MainVFXOffset
            : action.HitOffset + action.HitVFXOffset;
        instance.transform.localRotation = Quaternion.Euler(isMainVFX
            ? action.MainVFXEulerAngles
            : action.HitVFXEulerAngles);
        instance.transform.localScale = Vector3.Scale(
            prefab.transform.localScale,
            isMainVFX ? action.MainVFXScale : action.HitVFXScale);

        var preview = instance.AddComponent<WeaponAttackVFXPreview>();
        preview.Init(action, isMainVFX);
        Selection.activeGameObject = instance;
    }

    private static bool TryFindRuntimeWeapon(WeaponAttackSO action, out WeaponBase weapon)
    {
        var weapons = Object.FindObjectsByType<WeaponBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var candidate in weapons)
        {
            if (candidate == null || candidate.AttackActions == null)
                continue;

            foreach (var attack in candidate.AttackActions)
            {
                if (attack == action)
                {
                    weapon = candidate;
                    return true;
                }
            }
        }

        weapon = null;
        return false;
    }

    private static bool TryCalculateScale(
        WeaponAttackSO action,
        ParticleSystem prefab,
        out Vector3 scale)
    {
        scale = Vector3.one;
        if (action == null)
            return false;

        if (prefab == null)
        {
            Debug.LogWarning("VFX prefab is empty.", action);
            return false;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject;
        if (instance == null)
        {
            Debug.LogWarning($"Could not instantiate VFX prefab: {prefab.name}", prefab);
            return false;
        }

        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            if (TryCalculateRendererBounds(instance, out var bounds) == false)
            {
                Debug.LogWarning($"VFX prefab has no renderer bounds: {prefab.name}", prefab);
                return false;
            }

            var currentSize = MakeSafeSize(bounds.size);
            var targetSize = MakeSafeSize(action.Range);
            scale = new Vector3(
                targetSize.x / currentSize.x,
                targetSize.y / currentSize.y,
                targetSize.z / currentSize.z) * DefaultFitPadding;
            scale = ClampScaleMultiplier(scale, prefab.transform.localScale);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static bool TryCalculateRendererBounds(GameObject instance, out Bounds bounds)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private static Vector3 MakeSafeSize(Vector3 size)
    {
        return new Vector3(
            Mathf.Max(MinAxisSize, Mathf.Abs(size.x)),
            Mathf.Max(MinAxisSize, Mathf.Abs(size.y)),
            Mathf.Max(MinAxisSize, Mathf.Abs(size.z)));
    }

    private static Vector3 ClampScaleMultiplier(Vector3 multiplier, Vector3 baseScale)
    {
        return new Vector3(
            ClampScaleAxis(multiplier.x, baseScale.x),
            ClampScaleAxis(multiplier.y, baseScale.y),
            ClampScaleAxis(multiplier.z, baseScale.z));
    }

    private static float ClampScaleAxis(float multiplier, float baseScale)
    {
        if (Mathf.Abs(baseScale) < MinAxisSize)
            return 1f;

        return Mathf.Clamp(multiplier, MinScaleMultiplier, MaxScaleMultiplier);
    }

    private static void ApplyChange(WeaponAttackSO action, System.Action change)
    {
        if (action == null || change == null)
            return;

        Undo.RecordObject(action, "Update Weapon Attack VFX Fit");
        change.Invoke();
        EditorUtility.SetDirty(action);
    }

    private static Vector3 GetPrefabScale(ParticleSystem prefab)
    {
        return prefab != null ? prefab.transform.localScale : Vector3.one;
    }

    private static Vector3 DivideScale(Vector3 value, Vector3 divisor)
    {
        return new Vector3(
            DivideAxis(value.x, divisor.x),
            DivideAxis(value.y, divisor.y),
            DivideAxis(value.z, divisor.z));
    }

    private static float DivideAxis(float value, float divisor)
    {
        return Mathf.Abs(divisor) < MinAxisSize ? value : value / divisor;
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
#endif
