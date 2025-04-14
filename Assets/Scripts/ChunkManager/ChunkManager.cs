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
                    // ���ο� ���ؼ� üũ
                    currentChunk.CheckDirection.Add(dir);
                    neighborChunk.CheckDirection.Add(-dir);

                    // ���� ��, �� ûũ�� �� ������ Ȯ���Ǿ����Ƿ� �޽� ���ļ� ����ȭ
                    StartCoroutine(CombineMesh(currentChunk));
                    StartCoroutine(CombineMesh(neighborChunk));
                }
            }
        }
    }

    private IEnumerator CombineMesh(Chunk chunk)
    {
        // �̹� �����߰ų� �������̸� �Լ� ����
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

            // �ڱ� �ڽ��̸� ����
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

            // ���� �޽��� ��Ȱ��ȭ
            filter.mesh = null;
        }

        // �� �޽� ����
        if (combineWall.Count > 0)
        {
            Mesh wallMesh = new Mesh();
            wallMesh.CombineMeshes(combineWall.ToArray(), true, true);

            var wallFilter = wallObject.AddComponent<MeshFilter>();
            var wallRenderer = wallObject.AddComponent<MeshRenderer>();

            wallFilter.mesh = wallMesh;
            wallRenderer.sharedMaterial = wallMaterial; // �ʿ��� ��Ƽ���� �Ҵ�

            wallObject.layer = wallLayer;
        }

        // �ٴ� �޽� ����
        if (combineFloor.Count > 0)
        {
            Mesh floorMesh = new Mesh();
            floorMesh.CombineMeshes(combineFloor.ToArray(), true, true);

            var floorFilter = floorObject.AddComponent<MeshFilter>();
            var floorRenderer = floorObject.AddComponent<MeshRenderer>();

            floorFilter.mesh = floorMesh;
            floorRenderer.sharedMaterial = floorMaterial; // �ʿ��� ��Ƽ���� �Ҵ�

            floorObject.layer = floorLayer;
        }
        yield break;
    }


}
