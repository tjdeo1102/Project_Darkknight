using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    [Serializable]
    public struct TileSst
    {
        public TileType type;
        public ObjectPool<Transform> pool;
        public Vector3 offset;
    }

    public List<TileSst> Tiles;
    public bool IsReady { get; private set; }

    private Dictionary<TileType, TileSst> m_tiles;
    private readonly Dictionary<Transform, ObjectPool<Transform>> m_tileOwners = new();
    private readonly Collider[] m_overlapBuffer = new Collider[64];
    private const int PoolWarmupPerFrame = 64;
    private const int TilePlacementsPerFrame = 64;

    private void Awake()
    {
        InitTiles();
    }

    private IEnumerator Start()
    {
        foreach (var tile in m_tiles.Values)
        {
            yield return tile.pool.Warmup(PoolWarmupPerFrame);
        }

        IsReady = true;
    }

    private void InitTiles()
    {
        if (m_tiles != null) return;

        m_tiles = new Dictionary<TileType, TileSst>();
        if (Tiles == null) return;

        foreach (var t in Tiles)
        {
            if (t.pool == null) continue;

            t.pool.Init(null, 0);
            m_tiles[t.type] = t;
        }
    }

    public IEnumerator GenerateChunkRoutine(
        RectInt bounds,
        Transform parent,
        int minRoomSize,
        Vector3Int blockSize,
        Action<List<Vector3>> onComplete)
    {
        InitTiles();

        var width = bounds.width;
        var height = bounds.height;
        var mapData = new TileType[width, height];
        var floorPosData = new List<Vector3>();
        var placementCount = 0;

        var root = new BSPNode(new RectInt(0, 0, width, height));
        root.Split(minRoomSize);

        foreach (var room in root.GetRooms())
        {
            for (var x = room.xMin; x < room.xMax; x++)
            {
                for (var y = room.yMin; y < room.yMax; y++)
                {
                    if (IsInside(mapData, x, y))
                    {
                        mapData[x, y] = TileType.Floor;
                    }
                }
            }
        }

        ConnectRooms(root, mapData);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (mapData[x, y] != TileType.Floor) continue;

                var pos = ToWorldPosition(bounds, x, y, blockSize);
                if (m_tiles.TryGetValue(TileType.Floor, out var floor))
                {
                    var obj = GetTileObject(floor.pool);
                    obj.parent = parent;
                    obj.position = pos;
                    floorPosData.Add(pos);
                    placementCount++;
                }

                if (m_tiles.TryGetValue(TileType.Celling, out var ceiling))
                {
                    var obj = GetTileObject(ceiling.pool);
                    obj.position = pos + ceiling.offset;
                    obj.parent = parent;
                    placementCount++;
                }

                if (placementCount >= TilePlacementsPerFrame)
                {
                    placementCount = 0;
                    yield return null;
                }
            }
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (mapData[x, y] != TileType.Empty) continue;

                var pos = ToWorldPosition(bounds, x, y, blockSize);
                if (m_tiles.TryGetValue(TileType.Wall, out var wall))
                {
                    var obj = GetTileObject(wall.pool);
                    obj.position = pos + wall.offset;
                    obj.parent = parent;
                    placementCount++;
                }

                mapData[x, y] = TileType.Wall;
                if (placementCount >= TilePlacementsPerFrame)
                {
                    placementCount = 0;
                    yield return null;
                }
            }
        }

        for (var x = 0; x <= width; x++)
        {
            for (var y = -1; y < height; y++)
            {
                if (CreatePillar(mapData, x, y, ToWorldPosition(bounds, x, y, blockSize), parent))
                {
                    placementCount++;
                }

                if (placementCount >= TilePlacementsPerFrame)
                {
                    placementCount = 0;
                    yield return null;
                }
            }
        }

        onComplete?.Invoke(floorPosData);
    }

    private Vector3 ToWorldPosition(RectInt bounds, int x, int y, Vector3Int blockSize)
    {
        return new Vector3(bounds.x + x * blockSize.x, 0, bounds.y + y * blockSize.z);
    }

    private bool IsInside(TileType[,] map, int x, int y)
    {
        return x >= 0 && y >= 0 && x < map.GetLength(0) && y < map.GetLength(1);
    }

    private bool CreatePillar(TileType[,] map, int x, int y, Vector3 wallPos, Transform parent)
    {
        var current = IsWall(map, x, y);
        var left = IsWall(map, x - 1, y);
        var up = IsWall(map, x, y + 1);
        var leftUp = IsWall(map, x - 1, y + 1);
        var wallCount = (current ? 1 : 0)
                        + (left ? 1 : 0)
                        + (up ? 1 : 0)
                        + (leftUp ? 1 : 0);
        var shouldCreate = wallCount == 1 || wallCount == 3;

        if (shouldCreate && m_tiles.TryGetValue(TileType.Pillar, out var pillar))
        {
            var obj = GetTileObject(pillar.pool);
            obj.position = wallPos + pillar.offset;
            obj.parent = parent;
            return true;
        }

        return false;
    }

    private bool IsWall(TileType[,] map, int x, int y)
    {
        return IsInside(map, x, y) && map[x, y] == TileType.Wall;
    }

    private void ConnectRooms(BSPNode node, TileType[,] mapData)
    {
        if (node == null || node.IsLeaf || node.Left == null || node.Right == null) return;

        var centerA = node.Left.GetRoomCenter();
        var centerB = node.Right.GetRoomCenter();

        if (Random.value > 0.5f)
        {
            CarveHorizontal(mapData, centerA.x, centerB.x, centerA.y);
            CarveVertical(mapData, centerA.y, centerB.y, centerB.x);
        }
        else
        {
            CarveVertical(mapData, centerA.y, centerB.y, centerA.x);
            CarveHorizontal(mapData, centerA.x, centerB.x, centerB.y);
        }

        ConnectRooms(node.Left, mapData);
        ConnectRooms(node.Right, mapData);
    }

    private void CarveHorizontal(TileType[,] mapData, int fromX, int toX, int y)
    {
        for (var x = Mathf.Min(fromX, toX); x <= Mathf.Max(fromX, toX); x++)
        {
            if (IsInside(mapData, x, y))
            {
                mapData[x, y] = TileType.Floor;
            }
        }
    }

    private void CarveVertical(TileType[,] mapData, int fromY, int toY, int x)
    {
        for (var y = Mathf.Min(fromY, toY); y <= Mathf.Max(fromY, toY); y++)
        {
            if (IsInside(mapData, x, y))
            {
                mapData[x, y] = TileType.Floor;
            }
        }
    }

    public bool TryConnectChunks(Chunk chunk, Chunk neighborChunk, Vector2Int chunkToNeighborDir, Vector3Int blockSize)
    {
        if (chunk == null || neighborChunk == null || blockSize.x <= 0 || blockSize.z <= 0) return false;
        if (chunkToNeighborDir != Vector2Int.up &&
            chunkToNeighborDir != Vector2Int.down &&
            chunkToNeighborDir != Vector2Int.left &&
            chunkToNeighborDir != Vector2Int.right) return false;

        return TryCarveFallbackTunnel(chunk, neighborChunk, chunkToNeighborDir, blockSize);
    }

    private bool TryCarveFallbackTunnel(Chunk chunk, Chunk neighborChunk, Vector2Int dir, Vector3Int blockSize)
    {
        if (chunk.floorPosData == null || neighborChunk.floorPosData == null) return false;
        if (chunk.floorPosData.Count == 0 || neighborChunk.floorPosData.Count == 0) return false;

        var start = GetEdgeFloor(chunk, dir);
        var end = GetEdgeFloor(neighborChunk, -dir);
        CarveGridTunnel(chunk, neighborChunk, start, end, dir, blockSize);
        return true;
    }

    private Vector3 GetEdgeFloor(Chunk chunk, Vector2Int dir)
    {
        var floors = chunk.floorPosData;
        var best = floors[0];
        var selected = best;
        var selectedCount = 1;

        for (var i = 1; i < floors.Count; i++)
        {
            var pos = floors[i];
            if (IsCloserToEdge(pos, best, dir))
            {
                best = pos;
                selected = pos;
                selectedCount = 1;
                continue;
            }

            if (IsSameEdge(pos, best, dir) == false) continue;

            selectedCount++;
            if (Random.Range(0, selectedCount) == 0)
            {
                selected = pos;
            }
        }

        return selected;
    }

    private bool IsCloserToEdge(Vector3 candidate, Vector3 currentBest, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return candidate.z > currentBest.z;
        if (dir == Vector2Int.down) return candidate.z < currentBest.z;
        if (dir == Vector2Int.right) return candidate.x > currentBest.x;
        return candidate.x < currentBest.x;
    }

    private bool IsSameEdge(Vector3 candidate, Vector3 currentBest, Vector2Int dir)
    {
        if (dir == Vector2Int.up || dir == Vector2Int.down)
        {
            return Mathf.Approximately(candidate.z, currentBest.z);
        }

        return Mathf.Approximately(candidate.x, currentBest.x);
    }

    private void CarveGridTunnel(Chunk chunk, Chunk neighborChunk, Vector3 start, Vector3 end, Vector2Int dir, Vector3Int blockSize)
    {
        var current = start;
        var forwardStep = new Vector3(dir.x * blockSize.x, 0f, dir.y * blockSize.z);
        var sideStep = dir.x == 0 ? new Vector3(Mathf.Sign(end.x - start.x) * blockSize.x, 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(end.z - start.z) * blockSize.z);
        var carvedCells = new HashSet<Vector3>();

        CarveTunnelCell(chunk, neighborChunk, current, blockSize, carvedCells);

        if (dir.x == 0)
        {
            while (Mathf.Approximately(current.z, end.z) == false)
            {
                current += forwardStep;
                if ((dir.y > 0 && current.z > end.z) || (dir.y < 0 && current.z < end.z))
                {
                    current.z = end.z;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize, carvedCells);
            }

            while (Mathf.Approximately(current.x, end.x) == false)
            {
                current += sideStep;
                if ((sideStep.x > 0f && current.x > end.x) || (sideStep.x < 0f && current.x < end.x))
                {
                    current.x = end.x;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize, carvedCells);
            }
        }
        else
        {
            while (Mathf.Approximately(current.x, end.x) == false)
            {
                current += forwardStep;
                if ((dir.x > 0 && current.x > end.x) || (dir.x < 0 && current.x < end.x))
                {
                    current.x = end.x;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize, carvedCells);
            }

            while (Mathf.Approximately(current.z, end.z) == false)
            {
                current += sideStep;
                if ((sideStep.z > 0f && current.z > end.z) || (sideStep.z < 0f && current.z < end.z))
                {
                    current.z = end.z;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize, carvedCells);
            }
        }

        RebuildTunnelPillars(chunk, neighborChunk, carvedCells, blockSize);
    }

    private void CarveTunnelCell(
        Chunk chunk,
        Chunk neighborChunk,
        Vector3 pos,
        Vector3Int blockSize,
        ISet<Vector3> carvedCells)
    {
        var owner = ContainsWorldPosition(chunk, pos, blockSize) ? chunk : neighborChunk;
        RemoveBlockingTiles(pos, blockSize);
        if (HasTileAt(pos, TileType.Floor, blockSize) == false)
        {
            CreateTunnelFloor(pos, owner.ChunkObject.transform);
        }

        owner.floorPosData ??= new List<Vector3>();
        if (HasFloorData(owner.floorPosData, pos) == false)
        {
            owner.floorPosData.Add(pos);
        }

        carvedCells.Add(pos);
    }

    private bool HasFloorData(List<Vector3> floors, Vector3 pos)
    {
        for (var i = 0; i < floors.Count; i++)
        {
            if (Vector3.SqrMagnitude(floors[i] - pos) < 0.01f)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasTileAt(Vector3 pos, TileType tileType, Vector3Int blockSize)
    {
        var layer = LayerMask.NameToLayer(tileType.ToString());
        if (layer < 0) return false;

        var halfExtents = new Vector3(blockSize.x * 0.45f, 1.5f, blockSize.z * 0.45f);
        return Physics.OverlapBoxNonAlloc(pos + Vector3.up * 0.5f, halfExtents, m_overlapBuffer, Quaternion.identity, 1 << layer) > 0;
    }

    private bool ContainsWorldPosition(Chunk chunk, Vector3 pos, Vector3Int blockSize)
    {
        var minX = chunk.Bounds.xMin;
        var minZ = chunk.Bounds.yMin;
        var maxX = chunk.Bounds.xMin + chunk.Bounds.width * blockSize.x;
        var maxZ = chunk.Bounds.yMin + chunk.Bounds.height * blockSize.z;

        return pos.x >= minX && pos.x < maxX && pos.z >= minZ && pos.z < maxZ;
    }

    private void RemoveBlockingTiles(Vector3 pos, Vector3Int blockSize)
    {
        var wallLayer = LayerMask.NameToLayer(TileType.Wall.ToString());
        var gateLayer = LayerMask.NameToLayer(TileType.Gate.ToString());
        var mask = 0;

        if (wallLayer >= 0) mask |= 1 << wallLayer;
        if (gateLayer >= 0) mask |= 1 << gateLayer;
        if (mask == 0) mask = Physics.DefaultRaycastLayers;

        var halfExtents = new Vector3(blockSize.x * 0.45f, 1.5f, blockSize.z * 0.45f);
        var hitCount = Physics.OverlapBoxNonAlloc(pos + Vector3.up * 0.5f, halfExtents, m_overlapBuffer, Quaternion.identity, mask);
        for (var i = 0; i < hitCount; i++)
        {
            var blockingObject = m_overlapBuffer[i].gameObject;
            if (TryReturnTile(blockingObject.transform) == false)
                Destroy(blockingObject);
        }
    }

    private void RebuildTunnelPillars(
        Chunk chunk,
        Chunk neighborChunk,
        IEnumerable<Vector3> carvedCells,
        Vector3Int blockSize)
    {
        if (m_tiles.TryGetValue(TileType.Pillar, out var pillar) == false) return;

        var halfX = blockSize.x * 0.5f;
        var halfZ = blockSize.z * 0.5f;
        var vertices = new HashSet<Vector3>();
        foreach (var cell in carvedCells)
        {
            vertices.Add(new Vector3(cell.x - halfX, 0f, cell.z - halfZ));
            vertices.Add(new Vector3(cell.x - halfX, 0f, cell.z + halfZ));
            vertices.Add(new Vector3(cell.x + halfX, 0f, cell.z - halfZ));
            vertices.Add(new Vector3(cell.x + halfX, 0f, cell.z + halfZ));
        }

        Physics.SyncTransforms();
        foreach (var vertex in vertices)
        {
            RemovePillarAt(vertex);

            var wallCount = 0;
            if (HasTileAt(vertex + new Vector3(-halfX, 0f, -halfZ), TileType.Wall, blockSize)) wallCount++;
            if (HasTileAt(vertex + new Vector3(-halfX, 0f, halfZ), TileType.Wall, blockSize)) wallCount++;
            if (HasTileAt(vertex + new Vector3(halfX, 0f, -halfZ), TileType.Wall, blockSize)) wallCount++;
            if (HasTileAt(vertex + new Vector3(halfX, 0f, halfZ), TileType.Wall, blockSize)) wallCount++;

            if (wallCount != 1 && wallCount != 3) continue;

            var owner = ContainsWorldPosition(chunk, vertex, blockSize)
                ? chunk
                : neighborChunk;
            var obj = GetTileObject(pillar.pool);
            obj.SetParent(owner.ChunkObject.transform, true);
            obj.position = vertex + Vector3.up * pillar.offset.y;
        }
    }

    private void RemovePillarAt(Vector3 vertex)
    {
        var pillarLayer = LayerMask.NameToLayer(TileType.Pillar.ToString());
        if (pillarLayer < 0) return;

        var hitCount = Physics.OverlapBoxNonAlloc(
            vertex + Vector3.up * 1.5f,
            new Vector3(0.6f, 2f, 0.6f),
            m_overlapBuffer,
            Quaternion.identity,
            1 << pillarLayer);
        for (var i = 0; i < hitCount; i++)
        {
            var pillarObject = m_overlapBuffer[i].gameObject;
            if (TryReturnTile(pillarObject.transform) == false)
                Destroy(pillarObject);
        }
    }

    private Vector2 GetChunkEdgeStart(Chunk chunk, Vector3Int blockSize, Vector2Int tunnelDir)
    {
        var minX = chunk.ChunkObject.transform.position.x;
        var minZ = chunk.ChunkObject.transform.position.z;
        var maxX = minX + chunk.Bounds.width * blockSize.x;
        var maxZ = minZ + chunk.Bounds.height * blockSize.z;

        if (tunnelDir == Vector2Int.up) return new Vector2(minX, maxZ);
        if (tunnelDir == Vector2Int.down) return new Vector2(minX, minZ);
        if (tunnelDir == Vector2Int.left) return new Vector2(minX, minZ);
        if (tunnelDir == Vector2Int.right) return new Vector2(maxX, minZ);

        return new Vector2(minX, minZ);
    }

    private bool TryBreakSlimWall(Vector2 minEdge, Vector2Int tunnelDir, Vector3 wallDir, Vector3Int blockSize, int thickness, int maxLength, Transform parent)
    {
        InitTiles();

        var blockFrontSize = wallDir == Vector3.right ? blockSize.z : blockSize.x;
        var blockRightSize = wallDir == Vector3.right ? blockSize.x : blockSize.z;
        var rayDir = new Vector3(tunnelDir.x, 0, tunnelDir.y);
        var startXY = minEdge - (Vector2)tunnelDir * (thickness * blockFrontSize);
        var startTunnelOffset = new Vector3(startXY.x, 0, startXY.y);

        if (m_tiles.TryGetValue(TileType.Wall, out var wall))
        {
            startTunnelOffset += wall.offset.y * Vector3.up;
        }

        var wallLayer = LayerMask.NameToLayer("Wall");
        var wallMask = wallLayer >= 0 ? 1 << wallLayer : Physics.DefaultRaycastLayers;
        var rayDistance = (thickness * 2 + 1) * blockFrontSize;

        Physics.SyncTransforms();

        if (Physics.Raycast(startTunnelOffset, rayDir, out var hit, rayDistance, wallMask))
        {
            startTunnelOffset = hit.transform.position - rayDir * thickness * blockFrontSize;
        }

        Vector3 bestStartPos = Vector3.zero;
        RaycastHit[] bestHits = null;

        var startIndex = Random.Range(0, maxLength);
        for (var attempt = 0; attempt < maxLength; attempt++)
        {
            var wallIndex = (startIndex + attempt) % maxLength;
            var startPos = startTunnelOffset + wallDir * blockRightSize * wallIndex;
            var hits = Physics.RaycastAll(startPos, rayDir, rayDistance, wallMask);
            if (hits.Length == 0 || hits.Length > thickness) continue;

            CarveTunnel(startPos, rayDir, rayDistance, hits, parent);
            return true;
        }

        for (var attempt = 0; attempt < maxLength; attempt++)
        {
            var wallIndex = (startIndex + attempt) % maxLength;
            var startPos = startTunnelOffset + wallDir * blockRightSize * wallIndex;
            var hits = Physics.RaycastAll(startPos, rayDir, rayDistance, wallMask);
            if (hits.Length == 0) continue;

            if (bestHits == null || hits.Length < bestHits.Length)
            {
                bestStartPos = startPos;
                bestHits = hits;
            }
        }

        if (bestHits != null)
        {
            CarveTunnel(bestStartPos, rayDir, rayDistance, bestHits, parent);
            return true;
        }

        return false;
    }

    private void CarveTunnel(Vector3 startPos, Vector3 rayDir, float rayDistance, RaycastHit[] hits, Transform parent)
    {
        Debug.DrawRay(startPos, rayDir * rayDistance, Color.red, 3000f);

        var tunnelLength = 0f;
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hitInfo in hits)
        {
            var pos = new Vector3(hitInfo.transform.position.x, 0, hitInfo.transform.position.z);
            CreateTunnelFloor(pos, parent);
            tunnelLength = Mathf.Max(tunnelLength, hitInfo.distance);
            if (TryReturnTile(hitInfo.collider.transform) == false)
                Destroy(hitInfo.collider.gameObject);
        }

        // Gate generation is temporarily disabled.
        // CreateTunnelGates(startPos, rayDir, tunnelLength, parent);
    }

    private void CreateTunnelFloor(Vector3 pos, Transform parent)
    {
        if (m_tiles.TryGetValue(TileType.Floor, out var floor))
        {
            var obj = GetTileObject(floor.pool);
            obj.position = pos;
            obj.parent = parent;
        }

        if (m_tiles.TryGetValue(TileType.Celling, out var ceiling))
        {
            var obj = GetTileObject(ceiling.pool);
            obj.position = pos + ceiling.offset;
            obj.parent = parent;
        }
    }

    private void CreateTunnelGates(Vector3 startPos, Vector3 rayDir, float tunnelLength, Transform parent)
    {
        if (m_tiles.TryGetValue(TileType.Gate, out var gate) == false) return;

        var forward = rayDir;
        var right = Vector3.Cross(Vector3.up, forward);
        var up = Vector3.up;
        var rotation = Quaternion.LookRotation(rayDir);
        var gateOffset = gate.offset;
        var rotatedPos = startPos
                         + gateOffset.z * forward
                         + gateOffset.x * right
                         + gateOffset.y * up;

        var gate1 = GetTileObject(gate.pool);
        gate1.position = rotatedPos;
        gate1.rotation = rotation;
        gate1.parent = parent;

        var gate2 = GetTileObject(gate.pool);
        gate2.position = rotatedPos + tunnelLength * rayDir;
        gate2.rotation = rotation;
        gate2.parent = parent;
    }

    public void ReleaseChunkTiles(Transform chunkRoot)
    {
        if (chunkRoot == null) return;

        var transforms = chunkRoot.GetComponentsInChildren<Transform>(true);
        foreach (var tile in transforms)
        {
            if (tile == chunkRoot) continue;
            ReturnTrackedTile(tile);
        }
    }

    private Transform GetTileObject(ObjectPool<Transform> pool)
    {
        var tile = pool.GetObject();
        m_tileOwners[tile] = pool;

        foreach (var renderer in tile.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        return tile;
    }

    private bool TryReturnTile(Transform candidate)
    {
        var current = candidate;
        while (current != null)
        {
            if (ReturnTrackedTile(current))
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool ReturnTrackedTile(Transform tile)
    {
        if (tile == null || m_tileOwners.TryGetValue(tile, out var pool) == false)
            return false;

        pool.ReturnObject(tile);
        return true;
    }
}
