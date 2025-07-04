using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ChunkManager : MonoBehaviour
{
    public int ChunkSize = 16;
    public int ViewRadius = 2;
    public int MinRoomSize = 6;
    public Vector3Int BlockSize = Vector3Int.one;

    public Transform Player;
    public Vector3 PlayerSpawnOffset = new Vector3(0 , 2 , 0);
    public MapGenerator Generator;

    public static ChunkManager Instance;

    private Vector2Int m_playerChunk;
    private Dictionary<Vector2Int, Chunk> m_chunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int m_lastPlayerChunk;
    [SerializeField] private NavMeshSurface m_surface;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            if (Player != null)
            {
                StartCoroutine(PlayerStartRoutine());
            }
        }
        else
        {
            Destroy(this);
        }
    }
    void Update()
    {
        m_playerChunk = new Vector2Int(
            Mathf.FloorToInt(Player.position.x / (ChunkSize * BlockSize.x)),
            Mathf.FloorToInt(Player.position.z / (ChunkSize * BlockSize.z))
        );

        foreach (var chunk in m_chunks)
        {
            float dist = Vector2Int.Distance(m_playerChunk, chunk.Key);
            if (dist <= ViewRadius * 2)
            {
                // 생성 안된 경우는 일단 생성
                if (!chunk.Value.IsGenerate)
                {
                    chunk.Value.Generate(MinRoomSize,BlockSize);
                }
                // 생성된 청크일 때, 시야 거리 밖 청크는 언로드
                else
                {
                    if (dist > ViewRadius) chunk.Value.Unload();
                    else
                    {
                        chunk.Value.Load();
                        ConnectNeighborChunk(chunk.Key, BlockSize);
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
                Vector2Int coord = m_playerChunk + new Vector2Int(x, y);
                if (!m_chunks.ContainsKey(coord))
                {
                    m_chunks[coord] = new Chunk(coord, ChunkSize, BlockSize,transform);
                }
            }
        }

        if (m_playerChunk != m_lastPlayerChunk)
        {
            // 맵 업데이트
            if (m_surface != null)
            {
                //m_surface.UpdateNavMesh(m_surface.navMeshData);
                //StartCoroutine(UpdateMapRoutine());
                UpdateMapRoutine().Forget();
            }
        }
        m_lastPlayerChunk = m_playerChunk;
    }

    public async UniTaskVoid UpdateMapRoutine()
    {
        var data = m_surface.navMeshData;
        var setting = m_surface.GetBuildSettings();
        List<NavMeshBuildMarkup> markups = new();
        int count = 0;
        int batchSize = 50;

        foreach (var chunk in m_chunks)
        {
            if (chunk.Value.IsLoaded)
            {
                var modifiers = chunk.Value.navMeshModifiers;
                foreach (var mod in modifiers)
                {
                    if (mod == null) continue;

                    var item = new NavMeshBuildMarkup
                    {
                        root = mod.transform,
                        overrideArea = mod.overrideArea,
                        area = mod.area,
                        ignoreFromBuild = mod.ignoreFromBuild,
                    };
                    markups.Add(item);
                    count++;
                    if (count % batchSize == 0)
                        await UniTask.Yield();
                }
            }
        }
        count = 0;

        // modifier은 비활성화 한 채로, 수동으로 마크업 추가

        List<NavMeshBuildSource> sources = new();
        NavMeshBuilder.CollectSources(
            transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
        await UniTask.Yield();

        Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        worldToLocal = worldToLocal.inverse;
        await UniTask.Yield();

        var result = new Bounds();
        foreach (var src in sources)
        {
            switch (src.shape)
            {
                case NavMeshBuildSourceShape.Mesh:
                    {
                        var m = src.sourceObject as Mesh;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                        break;
                    }
                case NavMeshBuildSourceShape.Terrain:
                    {
#if NMC_CAN_ACCESS_TERRAIN
                        // Terrain pivot is lower/left corner - shift bounds accordingly
                        var t = src.sourceObject as TerrainData;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
#else
                        Debug.LogWarning("The NavMesh cannot be properly baked for the terrain because the necessary functionality is missing. Add the com.unity.modules.terrain package through the Package Manager.");
#endif
                        break;
                    }
                case NavMeshBuildSourceShape.Box:
                case NavMeshBuildSourceShape.Sphere:
                case NavMeshBuildSourceShape.Capsule:
                case NavMeshBuildSourceShape.ModifierBox:
                    result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                    break;
            }

            count++;
            if (count % batchSize == 0)
                await UniTask.Yield();
        }
        result.Expand(0.1f);

        var process = NavMeshBuilder.UpdateNavMeshDataAsync(data, setting, sources, result);
        //var process = m_surface.UpdateNavMesh(m_surface.navMeshData);
        while (!process.isDone) await UniTask.Yield();

        await UniTask.Yield();
    }

    private Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
    {
        var absAxisX = Abs(mat.MultiplyVector(Vector3.right));
        var absAxisY = Abs(mat.MultiplyVector(Vector3.up));
        var absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
        var worldPosition = mat.MultiplyPoint(bounds.center);
        var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
        return new Bounds(worldPosition, worldSize);
    }

    private Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    public IEnumerator PlayerStartRoutine()
    {
        while(true)
        {
            if (m_chunks.ContainsKey(m_playerChunk) && m_chunks[m_playerChunk].IsLoaded) break;
            yield return null;
        }
        var floor = m_chunks[m_playerChunk].floorPosData;
        Player.transform.position = floor[Random.Range(0, floor.Count)];

        yield return new WaitForSeconds(3f);
        if (m_surface != null)
            m_surface.BuildNavMesh();

    }
    public Vector3 GetSpawnPoint(float minDist, Vector3 spawnOffset)
    {
        List<Chunk> pickChunks = new List<Chunk>();
        Vector3 res = Vector3.zero;

        foreach (var chunk in m_chunks)
        {
            float dist = Vector2Int.Distance(m_playerChunk, chunk.Key);
            if (dist >= minDist && chunk.Value.IsLoaded)
            {
                pickChunks.Add(chunk.Value);
            }
        }
        if (pickChunks.Count > 0)
        {
            int pickNum = Random.Range(0, pickChunks.Count);
            var pickFloor = pickChunks[pickNum].floorPosData;
            if (pickFloor.Count > 0)
            {
                pickNum = Random.Range(0, pickFloor.Count);
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

        foreach (var chunk in m_chunks)
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

    private void ConnectNeighborChunk(Vector2Int chunkCoord, Vector3Int blockSIze)
    {
        Vector2Int[] dirs = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in dirs)
        {
            Vector2Int neighborCoord = chunkCoord + dir;
            if (m_chunks.ContainsKey(neighborCoord))
            {
                Chunk currentChunk = m_chunks[chunkCoord];
                Chunk neighborChunk = m_chunks[neighborCoord];

                if (currentChunk.IsLoaded && neighborChunk.IsLoaded)
                {
                    if (currentChunk.IsCheckClosedChunk(dir)) continue;
                    Generator.TryConnectChunks(
                        currentChunk.Bounds,
                        neighborChunk.Bounds,
                        blockSIze,
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
            "Floor","Wall","Pillar","Ceiling", "Gate"
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
