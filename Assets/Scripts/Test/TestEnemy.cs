using System.Collections;
using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    public float MinDist = 2f;
    public Vector3 SpawnOffset = new Vector3(0,0.5f,0);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(3f);
        transform.position = ChunkManager.Instance.GetSpawnPoint(MinDist, SpawnOffset);
        print("�� ��ġ ����");
        StartCoroutine(CheckChunkRoutine());
    }

    IEnumerator CheckChunkRoutine()
    {
        yield return new WaitForSeconds(3f);
        while (true)
        {
            print($"���� ���� Ȱ��ȭ ���� {ChunkManager.Instance.IsLoadedChunk(transform.position)}");
            yield return new WaitForSeconds(1f);
        }
    }
}
