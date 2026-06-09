using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Require Setting")]
    public int UnitCountPerChunk = 5;
    public int PoolSIze = 15;
    public EnemyType SelectSpawnEnemy;
    public List<EnemyController> AllEnemys;
    public List<GameObject> CinemaBoss;

    private Dictionary<EnemyType, ObjectPool<EnemyController>> m_allEnemys;
    private InGameLoop m_inGameLoop;

    List<Coroutine> m_allCoroutine;

    public void Init()
    {
        m_allEnemys = new();
        m_inGameLoop = InGameLoop.Instance;

        foreach (var enemy in AllEnemys)
        {
            var pool = new ObjectPool<EnemyController>();
            pool.poolObj = enemy;
            pool.InitSize = PoolSIze;
            pool.Init(transform);
            m_allEnemys.Add(enemy.EnemyType, pool);
        }

        m_allCoroutine = new();

        StartCoroutine(CreatePoolObjectRoutine());
    }

    private IEnumerator CreatePoolObjectRoutine()
    {
        foreach (var pool in m_allEnemys.Values)
        {
            m_allCoroutine.Add(StartCoroutine(pool.AutoCreateObjectPerFrame()));
            yield return null;
        }
    }

    public void RandomUnitSpawnPerChunk(Chunk chunk)
    {
        for (var i = 0; i < UnitCountPerChunk; i++)
        {
            if (ChunkManager.Instance == null || m_inGameLoop == null) break;
            Vector3 pos = Vector3.zero;
            var res = ChunkManager.Instance.TryGetSpawnPointOnChunk(chunk,Vector3.up,out pos);
            if (res == false) continue;

            var filter = m_allEnemys.Keys
                // 해당 키타입 Flag가 존재하는 것만 필터링
                .Where(k => SelectSpawnEnemy.HasFlag((EnemyType)(k)))
                .ToList();
            if (filter.Count < 1)
            {
                Debug.Log("선택된 타입의 적이 Dictionary에 등록되지 않음");
                break;
            }
            var selectKey = filter[Random.Range(0, filter.Count)];
            var pool = m_allEnemys[selectKey];
            var enemy = pool.GetObject(false);
            var trans = enemy.transform;
            trans.position = pos;
            trans.rotation = Quaternion.identity;
            enemy.Target = m_inGameLoop.Player.gameObject;
            enemy.gameObject.SetActive(true);
            if (enemy.enabled)
            {
                enemy.ChunkRefresh();
            }
        }
    }

    public void Spawn(EnemyType selectSpawnEnemy, Vector3 spawnPos, bool useBasePostion = false)
    {
        if (ChunkManager.Instance == null || m_inGameLoop == null) return;

        var filter = m_allEnemys.Keys
            // 해당 키타입 Flag가 존재하는 것만 필터링
            .Where(k => selectSpawnEnemy.HasFlag((EnemyType)(k)))
            .ToList();

        if (filter.Count < 1)
        {
            Debug.Log("선택된 타입의 적이 Dictionary에 등록되지 않음");
            return;
        }

        // 선택된 적들 모두 스폰
        foreach (var k in filter)
        {
            var pool = m_allEnemys[k];
            var enemy = pool.GetObject(false);
            var trans = enemy.transform;
            if (useBasePostion == false)
                trans.position = spawnPos;
            trans.rotation = Quaternion.identity;
            enemy.Target = m_inGameLoop.Player.gameObject;
            enemy.gameObject.SetActive(true);
            if (enemy.enabled)
            {
                enemy.ChunkRefresh();
            }
        }
    }

    public void CinemaBossSpawn(int idx)
    {
        if (idx < 0|| idx >= CinemaBoss.Count) return;

        Instantiate(CinemaBoss[idx]);
    }

    public void DestroyEnemy(EnemyController ctrl)
    {
        if (m_allEnemys.TryGetValue(ctrl.EnemyType, out var pool))
        {
            ctrl.AI.enabled = false;
            ctrl.Rigid.useGravity = false;
            pool.ReturnObject(ctrl);
        }
    }
}
