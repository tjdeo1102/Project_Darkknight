using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class ChunkManager : ManagerBase<ChunkManager>
{
    public int ChunkSize = 16;
    public float ViewRadius = 2;
    public int MinRoomSize = 6;
    public Vector3Int BlockSize = Vector3Int.one;
    public bool IsBlockUpdateMap = false;

    public Transform Player;
    public Vector3 PlayerSpawnOffset = new Vector3(0, 2, 0);
    public MapGenerator Generator;
    public InGameLoop GameLoop;

    private Vector2Int m_playerChunk;
    private readonly Dictionary<Vector2Int, Chunk> m_chunks = new();
    private readonly HashSet<Vector2Int> m_loadedCoords = new();
    private Vector2Int m_lastPlayerChunk;
    private bool m_hasPlayerChunk;
    private bool m_hasLastPlayerChunk;
    private bool m_isUpdatingNavMesh;
    private bool m_pendingNavMeshUpdate;
    private bool m_isNavMeshUpdateQueued;
    private readonly HashSet<Chunk> m_pendingSpawnChunks = new();
    private static readonly Vector2Int[] NeighborDirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    [SerializeField] private NavMeshSurface m_surface;

    private void Start()
    {
        GameLoop = InGameLoop.Instance;

        if (Player != null)
        {
            MapUpdate(true);
        }

        IsReady = true;
    }

    private void Update()
    {
        if (GameLoop == null || GameLoop.IsStartGame == false) return;

        if (IsBlockUpdateMap == false)
        {
            MapUpdate(false);
        }

        NavUpdate();
    }

    private void MapUpdate(bool init)
    {
        if (Player == null || Generator == null || ChunkSize <= 0 || BlockSize.x <= 0 || BlockSize.z <= 0) return;

        var nextPlayerChunk = GetPlayerChunkCoord(Player.position);
        if (init == false && m_hasPlayerChunk && nextPlayerChunk == m_playerChunk) return;

        m_playerChunk = nextPlayerChunk;
        m_hasPlayerChunk = true;

        var range = Mathf.CeilToInt(ViewRadius + 2);
        m_loadedCoords.Clear();
        for (var i = -range; i < range; i++)
        {
            for (var j = -range; j < range; j++)
            {
                var offset = new Vector2Int(i, j);
                var coord = m_playerChunk + offset;
                var inView = offset.sqrMagnitude < ViewRadius * ViewRadius;

                if (inView)
                {
                    var shouldQueueSpawn = init == false || offset != Vector2Int.zero;
                    LoadOrGenerateChunk(coord, shouldQueueSpawn);
                    m_loadedCoords.Add(coord);
                }
                else
                {
                    if (m_chunks.TryGetValue(coord, out var chunk))
                    {
                        chunk.Unload();
                    }
                }
            }
        }

        if (init) return;

        foreach (var pair in m_chunks)
        {
            if (m_loadedCoords.Contains(pair.Key) == false)
            {
                pair.Value.Unload();
            }
        }
    }

    private void LoadOrGenerateChunk(Vector2Int coord, bool queueSpawn)
    {
        var chunk = GetOrCreateChunk(coord);
        var didGenerate = false;

        if (chunk.IsGenerate == false)
        {
            chunk.Generate(MinRoomSize, BlockSize);
            didGenerate = true;
        }

        if (chunk.IsLoaded == false)
        {
            chunk.Load();
        }

        ConnectNeighborChunk(coord);

        if (didGenerate && queueSpawn == false)
        {
            chunk.IsGenerateMonster = true;
        }

        if (didGenerate && queueSpawn && chunk.IsGenerateMonster == false)
        {
            chunk.IsGenerateMonster = true;
            m_pendingSpawnChunks.Add(chunk);
            RequestNavMeshUpdate();
        }
    }

    private Chunk GetOrCreateChunk(Vector2Int coord)
    {
        if (m_chunks.TryGetValue(coord, out var chunk))
        {
            return chunk;
        }

        chunk = new Chunk(coord, ChunkSize, BlockSize, transform);
        m_chunks[coord] = chunk;
        return chunk;
    }

    private Vector2Int GetPlayerChunkCoord(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / (ChunkSize * BlockSize.x)),
            Mathf.FloorToInt(position.z / (ChunkSize * BlockSize.z))
        );
    }

    private void NavUpdate()
    {
        if (m_hasLastPlayerChunk == false || m_playerChunk != m_lastPlayerChunk)
        {
            RequestNavMeshUpdate();
        }

        m_lastPlayerChunk = m_playerChunk;
        m_hasLastPlayerChunk = true;
    }

    private void RequestNavMeshUpdate()
    {
        if (m_isNavMeshUpdateQueued) return;

        m_isNavMeshUpdateQueued = true;
        RunQueuedNavMeshUpdate().Forget();
    }

    private async UniTaskVoid RunQueuedNavMeshUpdate()
    {
        await UniTask.Yield();
        m_isNavMeshUpdateQueued = false;
        UpdateMapRoutine().Forget();
    }

    public async UniTaskVoid UpdateMapRoutine()
    {
        if (m_surface == null || m_surface.navMeshData == null)
        {
            ProcessPendingChunkSpawns();
            return;
        }

        if (m_isUpdatingNavMesh)
        {
            m_pendingNavMeshUpdate = true;
            return;
        }

        m_isUpdatingNavMesh = true;
        m_pendingNavMeshUpdate = false;

        var data = m_surface.navMeshData;
        var setting = m_surface.GetBuildSettings();
        var markups = new List<NavMeshBuildMarkup>();
        var count = 0;
        const int batchSize = 50;
        var snap = m_chunks.ToArray();

        foreach (var chunk in snap)
        {
            if (chunk.Value.IsLoaded == false) continue;

            var modifiers = chunk.Value.navMeshModifiers;
            if (modifiers == null) continue;

            foreach (var mod in modifiers)
            {
                if (mod == null) continue;

                markups.Add(new NavMeshBuildMarkup
                {
                    root = mod.transform,
                    overrideArea = mod.overrideArea,
                    area = mod.area,
                    ignoreFromBuild = mod.ignoreFromBuild,
                });

                count++;
                if (count % batchSize == 0)
                {
                    await UniTask.Yield();
                }
            }
        }

        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
        await UniTask.Yield();

        var worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        var result = new Bounds();
        count = 0;

        foreach (var src in sources)
        {
            switch (src.shape)
            {
                case NavMeshBuildSourceShape.Mesh:
                    if (src.sourceObject is Mesh mesh)
                    {
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, mesh.bounds));
                    }
                    break;
                case NavMeshBuildSourceShape.Terrain:
#if NMC_CAN_ACCESS_TERRAIN
                    if (src.sourceObject is TerrainData terrain)
                    {
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * terrain.size, terrain.size)));
                    }
#else
                    Debug.LogWarning("The NavMesh cannot be baked for terrain because the terrain module is unavailable.");
#endif
                    break;
                case NavMeshBuildSourceShape.Box:
                case NavMeshBuildSourceShape.Sphere:
                case NavMeshBuildSourceShape.Capsule:
                case NavMeshBuildSourceShape.ModifierBox:
                    result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                    break;
            }

            count++;
            if (count % batchSize == 0)
            {
                await UniTask.Yield();
            }
        }

        result.Expand(0.1f);
        var process = NavMeshBuilder.UpdateNavMeshDataAsync(data, setting, sources, result);
        while (process.isDone == false)
        {
            await UniTask.Yield();
        }

        m_isUpdatingNavMesh = false;
        if (m_pendingNavMeshUpdate)
        {
            RequestNavMeshUpdate();
            return;
        }

        ProcessPendingChunkSpawns();
    }

    private void ProcessPendingChunkSpawns()
    {
        if (m_pendingSpawnChunks.Count == 0) return;
        if (GameLoop == null || GameLoop.IsStartGame == false) return;

        var chunks = m_pendingSpawnChunks.ToArray();
        foreach (var chunk in chunks)
        {
            if (chunk == null || chunk.IsLoaded == false)
            {
                m_pendingSpawnChunks.Remove(chunk);
                continue;
            }

            if (CanSpawnOnChunk(chunk) == false) continue;

            GameLoop?.EnemySpawner?.RandomUnitSpawnPerChunk(chunk);
            GameLoop?.NPCSpawner?.RandomUnitSpawnPerChunk(chunk);
            m_pendingSpawnChunks.Remove(chunk);
        }
    }

    private bool CanSpawnOnChunk(Chunk chunk)
    {
        return TryGetSpawnPointOnChunk(chunk, Vector3.up, out _);
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
        while (true)
        {
            if (m_chunks.ContainsKey(m_playerChunk) && m_chunks[m_playerChunk].IsLoaded) break;
            yield return null;
        }

        var floor = m_chunks[m_playerChunk].floorPosData;
        if (floor == null || floor.Count == 0) yield break;

        Player.transform.position = floor[Random.Range(0, floor.Count)] + PlayerSpawnOffset;
        if (m_surface != null)
        {
            m_surface.BuildNavMesh();
            yield return new WaitForSeconds(3f);
            RequestNavMeshUpdate();
        }
    }

    public bool TryGetSpawnPointOnChunk(Vector3 pos, Vector3 spawnOffset, out Vector3 res)
    {
        return TryGetSpawnPointOnChunk(GetChunk(pos), spawnOffset, out res);
    }

    public bool TryGetSpawnPointOnChunk(Chunk chunk, Vector3 spawnOffset, out Vector3 res)
    {
        res = Vector3.zero;
        if (chunk == null || chunk.floorPosData == null || chunk.floorPosData.Count == 0)
        {
            return false;
        }

        const int maxAttempts = 12;
        for (var i = 0; i < maxAttempts; i++)
        {
            var pickNum = Random.Range(0, chunk.floorPosData.Count);
            var candidate = chunk.floorPosData[pickNum] + spawnOffset;

            if (NavMesh.SamplePosition(candidate, out var hit, Mathf.Max(BlockSize.x, BlockSize.z) * 2f, NavMesh.AllAreas))
            {
                res = hit.position;
                return true;
            }

            res = candidate;
        }

        return false;
    }

    public Vector3 GetRandomSpawnPoint(Vector3 center, float minDist, float maxDist)
    {
        var dir = Random.insideUnitCircle.normalized;
        var dist = Random.Range(minDist, maxDist);
        var point = (Vector3)(dir * dist) + center;
        return new Vector3(point.x, -999f, point.z);
    }

    public Chunk GetChunk(Vector2Int pos)
    {
        return m_chunks.TryGetValue(pos, out var res) ? res : null;
    }

    public Chunk GetChunk(Vector3 pos)
    {
        if (ChunkSize <= 0 || BlockSize.x <= 0 || BlockSize.z <= 0) return null;

        var currentChunk = new Vector2Int(
            Mathf.FloorToInt(pos.x / (ChunkSize * BlockSize.x)),
            Mathf.FloorToInt(pos.z / (ChunkSize * BlockSize.z))
        );
        return m_chunks.TryGetValue(currentChunk, out var res) ? res : null;
    }

    private void ConnectNeighborChunk(Vector2Int chunkCoord)
    {
        foreach (var dir in NeighborDirs)
        {
            var neighborCoord = chunkCoord + dir;
            if (m_chunks.ContainsKey(neighborCoord) == false) continue;

            var currentChunk = m_chunks[chunkCoord];
            var neighborChunk = m_chunks[neighborCoord];
            if (currentChunk.IsLoaded == false || neighborChunk.IsLoaded == false) continue;
            if (currentChunk.IsCheckClosedChunk(dir)) continue;

            var isConnected = Generator.TryConnectChunks(currentChunk, neighborChunk, dir, BlockSize);
            if (isConnected == false) continue;

            currentChunk.CheckDirection.Add(dir);
            neighborChunk.CheckDirection.Add(-dir);
            currentChunk.RefreshNavMeshModifiers();
            neighborChunk.RefreshNavMeshModifiers();
            StartCoroutine(CombineMesh(currentChunk));
            StartCoroutine(CombineMesh(neighborChunk));
        }
    }

    private IEnumerator CombineMesh(Chunk chunk)
    {
        if (chunk.IsCombineMesh || chunk.CheckDirection.Count != 4) yield break;
        chunk.IsCombineMesh = true;
        yield return null;

        var meshFilters = chunk.ChunkObject.GetComponentsInChildren<MeshFilter>();
        var tileNames = new List<string>();

        for (var i = 0; i < (int)TileType.Size; i++)
        {
            tileNames.Add(((TileType)i).ToString());
        }

        foreach (var tileName in tileNames)
        {
            var createObject = new GameObject(tileName);
            createObject.transform.parent = chunk.ChunkObject.transform;

            var combineList = new List<CombineInstance>();
            var layer = LayerMask.NameToLayer(tileName);
            Material mat = null;

            foreach (var filter in meshFilters)
            {
                if (filter.transform == chunk.ChunkObject.transform || filter.gameObject.layer != layer) continue;
                if (filter.sharedMesh == null) continue;

                var renderer = filter.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    mat ??= renderer.sharedMaterial;
                }

                combineList.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = filter.transform.localToWorldMatrix
                });
            }

            if (combineList.Count > 0)
            {
                var mesh = new Mesh();
                mesh.CombineMeshes(combineList.ToArray(), true, true);

                var filter = createObject.AddComponent<MeshFilter>();
                var renderer = createObject.AddComponent<MeshRenderer>();

                filter.mesh = mesh;
                renderer.sharedMaterial = mat;
                createObject.layer = layer;
            }

            yield return null;
        }
    }
}
