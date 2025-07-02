using System.Collections;
using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    public float MinDist = 2f;
    public Vector3 SpawnOffset = Vector3.up;
    public GameObject EnemyPref;
    public GameObject Player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            transform.position = ChunkManager.Instance.GetSpawnPoint(MinDist, SpawnOffset);
            var enemy = GameObject.Instantiate(EnemyPref, transform.position, Quaternion.identity);
            enemy.GetComponent<EnemyController>().Target = Player;
            print("스폰 적");

            yield return new WaitForSeconds(10f);
        }
    }

    //IEnumerator CheckChunkRoutine()
    //{
    //    while (!ChunkManager.Instance.IsLoadedChunk(transform.position))
    //    {
    //        yield return new WaitForSeconds(1f);
    //    }
    //    var enemy = GameObject.Instantiate(EnemyPref, transform.position, Quaternion.identity);
    //    enemy.GetComponent<EnemyController>().Target = Player;
    //    print("스폰 적");
    //}
}
