using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;

    [Serializable]
    public struct SkillVFXPool
    {
        [SerializeField] public VFX SkillType;
        [SerializeField] public ObjectPool<ParticleSystem> Pool;
    }

    private Dictionary<VFX, ObjectPool<ParticleSystem>> skillDic;

    [SerializeField] public SkillVFXPool[] Pools;


    private void Awake()
    {
        skillDic = new Dictionary<VFX, ObjectPool<ParticleSystem>>();

        foreach(var kvp in Pools)
        {
            skillDic.Add(kvp.SkillType, kvp.Pool);
            kvp.Pool.Init(transform);
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayVFX(VFX fx, Vector3 spawnPos, Quaternion rotation)
    {
        if (fx == VFX.None) return;

        StartCoroutine(VFXRoutine(fx, spawnPos,rotation));
    }

    private IEnumerator VFXRoutine(VFX fx, Vector3 spawnPos, Quaternion rotation)
    {
        if (fx == VFX.None) yield break;

        var ps = skillDic[fx].GetObject();
        ps.transform.position = spawnPos;
        ps.transform.rotation = rotation;
        ps.Play();

        yield return new WaitForSeconds(ps.main.duration + 0.2f);
        skillDic[fx].ReturnObject(ps);
    }
}
