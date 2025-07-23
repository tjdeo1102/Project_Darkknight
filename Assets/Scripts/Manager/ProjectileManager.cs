using System.Collections.Generic;
using System;
using UnityEngine;

public class ProjectileManager : ManagerBase<ProjectileManager>
{
    [Serializable]
    public struct ProjectilePool
    {
        [SerializeField] public ProjectileType Type;
        [SerializeField] public ObjectPool<Projectile> Pool;
    }

    public Dictionary<ProjectileType, ObjectPool<Projectile>> ProjectileDic;

    [SerializeField] public ProjectilePool[] Pools;


    protected override void Awake()
    {
        base.Awake();
        ProjectileDic = new();

        foreach (var kvp in Pools)
        {
            ProjectileDic.Add(kvp.Type, kvp.Pool);
            kvp.Pool.Init(transform);
            StartCoroutine(kvp.Pool.AutoCreateObjectPerFrame());
        }
        IsReady = true;
    }
}
