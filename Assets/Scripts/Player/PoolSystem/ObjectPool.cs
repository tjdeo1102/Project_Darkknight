using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ObjectPool<T> where T : Component
{
    public T poolObj;
    public Transform parent;
    public int InitSize;
    private Queue<T> pool;

    public void Init(Transform parent = null)
    {
        pool = new Queue<T>();
        if (this.parent == null) this.parent = parent;
        CreateObject(InitSize);
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
        if (pool == null) pool = new Queue<T>();

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
        if (pool == null) pool = new Queue<T>();

        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    public int Count()
    {
        if (pool == null)
        {
            return 0;
        }
        return pool.Count;
    }

    public IEnumerator AutoCreateObjectPerFrame()
    {
        while (true)
        {
            if (Count() < InitSize)
            {
                CreateObject(1);
            }
            yield return null;
        }
    }
}
