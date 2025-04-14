using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int ChunkSize = 16;
    public int ViewRadius = 2;
    public int MinRoomSize = 6;
    public GameObject FloorPrefab;
    public GameObject WallPrefab;
    public Transform Player;

    

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    void Update()
    {
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(Player.position.x / ChunkSize),
            Mathf.FloorToInt(Player.position.z / ChunkSize)
        );

        foreach (var chunk in chunks)
        {
            float dist = Vector2Int.Distance(playerChunk, chunk.Key);
            if (dist <= ViewRadius)
            {
                if (!chunk.Value.IsLoaded)
                {
                    chunk.Value.Load(FloorPrefab, WallPrefab, MinRoomSize);
                    ConnectNeighborChunk(chunk.Key);
                }

            }
            else
            {
                if (chunk.Value.IsLoaded)
                    chunk.Value.Unload();
            }
        }

        for (int x = -ViewRadius; x <= ViewRadius; x++)
        {
            for (int y = -ViewRadius; y <= ViewRadius; y++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, y);
                if (!chunks.ContainsKey(coord))
                {
                    chunks[coord] = new Chunk(coord, ChunkSize);
                }
            }
        }
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
                    MapGenerator.ConnectChunks(
                        currentChunk.Bounds,
                        neighborChunk.Bounds,
                        currentChunk.ChunkObject.transform,
                        FloorPrefab
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

        var wallObject = new GameObject("Wall");
        var floorObject = new GameObject("Floor");

        wallObject.transform.parent = chunk.ChunkObject.transform;
        floorObject.transform.parent = chunk.ChunkObject.transform;

        List<CombineInstance> combineWall = new List<CombineInstance>();
        List<CombineInstance> combineFloor = new List<CombineInstance>();

        var wallLayer = LayerMask.NameToLayer("Wall");
        var floorLayer = LayerMask.NameToLayer("Floor");
        Material wallMaterial = null;
        Material floorMaterial = null;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            var filter = meshFilters[i];

            // 자기 자신이면 제외
            if (filter.transform == chunk.ChunkObject.transform) continue;
            if (filter.sharedMesh == null) continue;

            CombineInstance ci = new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = filter.transform.localToWorldMatrix
            };
            var renderer = filter.GetComponent<MeshRenderer>();
            renderer.enabled = false;

            if (filter.gameObject.layer == wallLayer)
            {
                combineWall.Add(ci);
                if (wallMaterial == null)
                {
                    if (renderer != null)
                    {
                        wallMaterial = renderer.sharedMaterial;
                    }
                }
            }
            else if (filter.gameObject.layer == floorLayer)
            {
                combineFloor.Add(ci);
                if (floorMaterial == null)
                {
                    if (renderer != null)
                    {
                        floorMaterial = renderer.sharedMaterial;
                    }
                }
            }

            // 기존 메쉬는 비활성화
            filter.mesh = null;
        }

        // 벽 메쉬 결합
        if (combineWall.Count > 0)
        {
            Mesh wallMesh = new Mesh();
            wallMesh.CombineMeshes(combineWall.ToArray(), true, true);

            var wallFilter = wallObject.AddComponent<MeshFilter>();
            var wallRenderer = wallObject.AddComponent<MeshRenderer>();

            wallFilter.mesh = wallMesh;
            wallRenderer.sharedMaterial = wallMaterial; // 필요한 머티리얼 할당

            wallObject.layer = wallLayer;
        }

        // 바닥 메쉬 결합
        if (combineFloor.Count > 0)
        {
            Mesh floorMesh = new Mesh();
            floorMesh.CombineMeshes(combineFloor.ToArray(), true, true);

            var floorFilter = floorObject.AddComponent<MeshFilter>();
            var floorRenderer = floorObject.AddComponent<MeshRenderer>();

            floorFilter.mesh = floorMesh;
            floorRenderer.sharedMaterial = floorMaterial; // 필요한 머티리얼 할당

            floorObject.layer = floorLayer;
        }
        yield break;
    }


}
