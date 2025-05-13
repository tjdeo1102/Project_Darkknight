using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool<T> where T : Component
{
    [SerializeField] private T poolObj;
    [SerializeField] private Transform parent;
    [SerializeField] private int initSize;
    private Queue<T> pool;

    public void Init(Transform parent = null)
    {
        pool = new Queue<T>();

        CreateObject(initSize);
    }

    private void CreateObject(int size)
    {
        for (int i = 0; i < size; i++)
        {
            var obj = GameObject.Instantiate(poolObj, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T GetObject()
    {
        if (pool.Count < 1)
        {
            CreateObject(1);
        }

        var obj = pool.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void ReturnObject(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
