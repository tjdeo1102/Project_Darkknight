using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;
    private const int DefaultRuntimePoolSize = 2;

    [Serializable]
    public struct SkillVFXPool
    {
        [SerializeField] public VFX SkillType;
        [SerializeField] public ObjectPool<ParticleSystem> Pool;
    }

    [SerializeField] private float returnPadding = 0.2f;

    private Dictionary<VFX, ObjectPool<ParticleSystem>> m_skillPools;
    private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> m_runtimePrefabPools = new();
    private readonly HashSet<VFX> m_missingSkillWarnings = new();

    [SerializeField] public SkillVFXPool[] Pools;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        m_skillPools = new Dictionary<VFX, ObjectPool<ParticleSystem>>();
        if (Pools == null) return;

        foreach(var kvp in Pools)
        {
            if (kvp.SkillType == VFX.None || kvp.Pool == null || kvp.Pool.poolObj == null)
            {
                Debug.LogWarning($"{nameof(SkillEffectManager)} has an invalid VFX pool entry.", this);
                continue;
            }

            if (m_skillPools.ContainsKey(kvp.SkillType))
            {
                Debug.LogWarning($"{nameof(SkillEffectManager)} has a duplicated VFX pool entry: {kvp.SkillType}", this);
                continue;
            }

            m_skillPools.Add(kvp.SkillType, kvp.Pool);
            kvp.Pool.Init(transform);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayVFX(VFX fx, Vector3 spawnPos, Quaternion rotation)
    {
        if (fx == VFX.None) return;

        if (m_skillPools == null || m_skillPools.TryGetValue(fx, out var pool) == false)
        {
            WarnMissingSkillPool(fx);
            return;
        }

        StartCoroutine(VFXRoutine(pool, spawnPos, rotation));
    }

    public bool PlayVFX(
        ParticleSystem prefab,
        Vector3 spawnPos,
        Quaternion rotation,
        Vector3 scaleMultiplier)
    {
        if (prefab == null) return false;

        var pool = GetOrCreateRuntimePool(prefab);
        StartCoroutine(VFXRoutine(pool, spawnPos, rotation, scaleMultiplier));
        return true;
    }

    private IEnumerator VFXRoutine(
        ObjectPool<ParticleSystem> pool,
        Vector3 spawnPos,
        Quaternion rotation)
    {
        yield return VFXRoutine(pool, spawnPos, rotation, Vector3.one);
    }

    private IEnumerator VFXRoutine(
        ObjectPool<ParticleSystem> pool,
        Vector3 spawnPos,
        Quaternion rotation,
        Vector3 scaleMultiplier)
    {
        if (pool == null) yield break;

        var ps = pool.GetObject();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.transform.position = spawnPos;
        ps.transform.rotation = rotation;
        ps.transform.localScale = GetScaledPrefabSize(pool, scaleMultiplier);
        ps.Play();

        yield return new WaitForSeconds(GetReturnDelay(ps));
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        pool.ReturnObject(ps);
    }

    private Vector3 GetScaledPrefabSize(
        ObjectPool<ParticleSystem> pool,
        Vector3 scaleMultiplier)
    {
        if (pool?.poolObj == null)
            return scaleMultiplier;

        return Vector3.Scale(pool.poolObj.transform.localScale, scaleMultiplier);
    }

    private ObjectPool<ParticleSystem> GetOrCreateRuntimePool(ParticleSystem prefab)
    {
        if (m_runtimePrefabPools.TryGetValue(prefab, out var pool))
            return pool;

        pool = new ObjectPool<ParticleSystem>
        {
            poolObj = prefab,
            InitSize = DefaultRuntimePoolSize
        };
        pool.Init(transform);
        m_runtimePrefabPools.Add(prefab, pool);
        return pool;
    }

    private float GetReturnDelay(ParticleSystem particle)
    {
        var main = particle.main;
        return Mathf.Max(0.01f, main.duration + main.startLifetime.constantMax + returnPadding);
    }

    private void WarnMissingSkillPool(VFX fx)
    {
        if (m_missingSkillWarnings.Add(fx) == false) return;
        Debug.LogWarning($"{nameof(SkillEffectManager)} has no pool for VFX: {fx}", this);
    }
}
