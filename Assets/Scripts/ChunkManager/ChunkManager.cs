using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int ChunkSize = 16;
    public int ViewRadius = 2;
    public int MinRoomSize = 6;

    public Transform Player;

    public static ChunkManager Instance;

    private Vector2Int playerChunk;
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    void Update()
    {
        playerChunk = new Vector2Int(
            Mathf.FloorToInt(Player.position.x / ChunkSize),
            Mathf.FloorToInt(Player.position.z / ChunkSize)
        );

        foreach (var chunk in chunks)
        {
            float dist = Vector2Int.Distance(playerChunk, chunk.Key);
            if (dist <= ViewRadius * 2)
            {
                // 생성 안된 경우는 일단 생성
                if (!chunk.Value.IsGenerate)
                {
                    chunk.Value.Generate(MinRoomSize);
                }
                // 생성된 청크일 때, 시야 거리 밖 청크는 언로드
                else
                {
                    if (dist > ViewRadius) chunk.Value.Unload();
                    else
                    {
                        chunk.Value.Load();
                        ConnectNeighborChunk(chunk.Key);
                    }
                }
            }
            else
            {
                if (chunk.Value.IsLoaded) chunk.Value.Unload();
            }
        }

        for (int x = -ViewRadius * 2; x <= ViewRadius * 2; x++)
        {
            for (int y = -ViewRadius * 2; y <= ViewRadius * 2; y++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, y);
                if (!chunks.ContainsKey(coord))
                {
                    chunks[coord] = new Chunk(coord, ChunkSize);
                }
            }
        }
    }

    public Vector3 GetSpawnPoint(float minDist, Vector3 spawnOffset)
    {
        List<Chunk> pickChunks = new List<Chunk>();
        Vector3 res = Vector3.zero;

        foreach (var chunk in chunks)
        {
            float dist = Vector2Int.Distance(playerChunk, chunk.Key);
            if (dist >= minDist && chunk.Value.IsLoaded)
            {
                pickChunks.Add(chunk.Value);
            }
        }
        System.Random rand = new System.Random();
        if (pickChunks.Count > 0)
        {
            int pickNum = rand.Next(0, pickChunks.Count);
            var pickFloor = pickChunks[pickNum].floorPosData;
            if (pickFloor.Count > 0)
            {
                pickNum = rand.Next(0, pickFloor.Count);
                res = pickFloor[pickNum] + spawnOffset;
            }
        }
        // 예외 상황: Vector3.zero로 반환
        return res;
    }

    public bool IsLoadedChunk(Vector3 currentPos)
    {
        var currentChunk = new Vector2Int(
            Mathf.FloorToInt(currentPos.x / ChunkSize),
            Mathf.FloorToInt(currentPos.z / ChunkSize)
        );

        foreach (var chunk in chunks)
        {
            float dist = Vector2Int.Distance(currentChunk, chunk.Key);
            if (dist < 1)
            {
                return chunk.Value.IsLoaded;
            }
        }
        // 청크맵 생성이 안된경우 False
        return false;
    }

    private void ConnectNeighborChunk(Vector2Int chunkCoord)
    {
        Vector2Int[] dirs = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in dirs)
        {
            Vector2Int neighborCoord = chunkCoord + dir;
            if (chunks.ContainsKey(neighborCoord))
            {
                Chunk currentChunk = chunks[chunkCoord];
                Chunk neighborChunk = chunks[neighborCoord];

                if (currentChunk.IsLoaded && neighborChunk.IsLoaded)
                {
                    if (currentChunk.IsCheckClosedChunk(dir)) continue;
                    MapGenerator.Instance.TryConnectChunks(
                        currentChunk.Bounds,
                        neighborChunk.Bounds,
                        currentChunk.ChunkObject.transform
                    );
                    // 서로에 대해서 체크
                    currentChunk.CheckDirection.Add(dir);
                    neighborChunk.CheckDirection.Add(-dir);
                    // 끝난 후, 두 청크의 맵 구조는 확정되었으므로 메쉬 합쳐서 최적화
                    StartCoroutine(CombineMesh(currentChunk));
                    StartCoroutine(CombineMesh(neighborChunk));
                }
            }
        }
    }

    private IEnumerator CombineMesh(Chunk chunk)
    {
        // 이미 수행했거나 수행중이면 함수 리턴
        if (chunk.IsCombineMesh || chunk.CheckDirection.Count != 4) yield break;
        chunk.IsCombineMesh = true;
        yield return null;

        MeshFilter[] meshFilters = chunk.ChunkObject.GetComponentsInChildren<MeshFilter>();

        List<string> list = new List<string>()
        {
            "Floor","Wall","Pillar","Ceiling"
        };

        foreach(var item in list)
        {
            var createObject = new GameObject(item);
            createObject.transform.parent = chunk.ChunkObject.transform;

            List<CombineInstance> combineList = new List<CombineInstance>();
            var layer = LayerMask.NameToLayer(item);
            Material mat = null;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var filter = meshFilters[i];

                // 자기 자신이면 or 같은 레이어 아니면 제외
                if (filter.transform == chunk.ChunkObject.transform || filter.gameObject.layer != layer) continue;
                if (filter.sharedMesh == null) continue;

                CombineInstance ci = new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = filter.transform.localToWorldMatrix
                };
                var renderer = filter.GetComponent<MeshRenderer>();
                renderer.enabled = false;
                combineList.Add(ci);
                if (mat == null)
                {
                    if (renderer != null)
                    {
                        mat = renderer.sharedMaterial;
                    }
                }
                // 기존 메쉬는 비활성화
                filter.mesh = null;
            }

            if (combineList.Count > 0)
            {
                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combineList.ToArray(), true, true);

                var filter = createObject.AddComponent<MeshFilter>();
                var renderer = createObject.AddComponent<MeshRenderer>();

                filter.mesh = mesh;
                renderer.sharedMaterial = mat; // 필요한 머티리얼 할당

                createObject.layer = layer;

            }
        }
    }
}
