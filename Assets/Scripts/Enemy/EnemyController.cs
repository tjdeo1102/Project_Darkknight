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
    public Animator Anim;

    public GameObject Target;

    public Vector3 SpawnOffset = Vector3.up;
    public float ChunkRefreshDelay = 2f;

    private void Awake()
    {
        Rigid = GetComponent<Rigidbody>();
        Rigid.useGravity = false;
        Rigid.isKinematic = true;
        Anim = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        Rigid.useGravity = false;
        Rigid.isKinematic = true;
    }
    public void ChunkRefresh()
    {
        if (InGameLoop.Instance == null || InGameLoop.Instance.ChunkManager == null) return;

        var chunk = InGameLoop.Instance.ChunkManager.GetChunk(transform.position);

        // 泥?겕? ?④퍡 愿由?
        if (chunk != null)
        {
            transform.parent = chunk.ChunkObject.transform;
            if (Rigid.isKinematic == false)
                Rigid.linearVelocity = Vector3.zero;
            Rigid.useGravity = false;
            Rigid.isKinematic = true;
            AI.enabled = true;
            AI.EnsureNavAgentReady();
        }
        // ?뺥빐吏?泥?겕 ?꾩튂???녿뒗 ?곸? ?ㅼ떆 諛섑솚
        else
        {
            InGameLoop.Instance.EnemySpawner.DestroyEnemy(this);
        }
    }
}
