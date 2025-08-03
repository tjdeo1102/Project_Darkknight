using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Properties;
using UnityEditor;


[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public EnemyStat Model;
    public EnemyAI AI;
    public EnemyType EnemyType;
    public Rigidbody Rigid;
    public InGameLoop GameLoop;
    public Animator Anim;

    public GameObject Target;

    public Vector3 SpawnOffset = Vector3.up;
    public float ChunkRefreshDelay = 2f;
    private ChunkManager m_chunkManager;

    private void Awake()
    {
        Rigid = GetComponent<Rigidbody>();
        Rigid.useGravity = false;
        Anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (InGameLoop.Instance != null)
        {
            GameLoop = InGameLoop.Instance;
            m_chunkManager = GameLoop.ChunkManager;
        }
    }

    private void OnDisable()
    {
        Rigid.useGravity = false;
    }
    public void ChunkRefresh()
    {
        if (m_chunkManager == null) return;

        var chunk = m_chunkManager.GetChunk(transform.position);
        // 청크와 함께 관리
        if (chunk != null)
        {
            transform.parent = chunk.ChunkObject.transform;
            Rigid.useGravity = true;
            AI.enabled = true;
        }
        // 정해진 청크 위치에 없는 적은 다시 반환
        else
        {
            GameLoop.EnemySpawner.DestroyEnemy(this);
        }
    }
}
