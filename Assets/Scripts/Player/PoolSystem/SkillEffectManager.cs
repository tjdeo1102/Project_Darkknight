using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;

    [Serializable]
    public struct SkillPool
    {
        [SerializeField] public VFX skillType;
        [SerializeField] public ObjectPool<ParticleSystem> skillPool;
    }

    private Dictionary<VFX, ObjectPool<ParticleSystem>> skillDic;

    [SerializeField] public SkillPool[] skillPools;


    private void Awake()
    {
        skillDic = new Dictionary<VFX, ObjectPool<ParticleSystem>>();

        foreach(var kvp in skillPools)
        {
            skillDic.Add(kvp.skillType, kvp.skillPool);
            kvp.skillPool.Init(transform);
        }

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void PlayVFX(VFX fx, Vector3 spawnPos, Quaternion rotation, float delay)
    {
        if (fx == VFX.None) return;

        StartCoroutine(VFXRoutine(fx, spawnPos,rotation,delay));
    }

    private IEnumerator VFXRoutine(VFX fx, Vector3 spawnPos, Quaternion rotation, float delay)
    {
        if (fx == VFX.None) yield break;
        yield return new WaitForSeconds(delay);

        var ps = skillDic[fx].GetObject();
        ps.transform.position = spawnPos;
        ps.transform.rotation = rotation;
        ps.Play();

        yield return new WaitForSeconds(ps.main.duration + 0.2f);
        skillDic[fx].ReturnObject(ps);
    }
}
