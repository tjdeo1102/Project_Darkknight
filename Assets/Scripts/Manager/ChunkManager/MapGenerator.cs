using System;
using System.Collections.Generic;
using System.Linq;
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

    private Dictionary<TileType, TileSst> m_tiles;

    private void Awake()
    {
        InitTiles();
    }

    private void InitTiles()
    {
        if (m_tiles != null) return;

        m_tiles = new Dictionary<TileType, TileSst>();
        if (Tiles == null) return;

        foreach (var t in Tiles)
        {
            if (t.pool == null) continue;

            t.pool.Init();
            m_tiles[t.type] = t;
            StartCoroutine(t.pool.AutoCreateObjectPerFrame());
        }
    }

    public void GenerateChunk(RectInt bounds, Transform parent, int minRoomSize, Vector3Int blockSize, out List<Vector3> floorPosData)
    {
        InitTiles();

        var width = bounds.width;
        var height = bounds.height;
        var mapData = new TileType[width, height];
        floorPosData = new List<Vector3>();

        var root = new BSPNode(new RectInt(0, 0, width, height));
        root.Split(minRoomSize);

        foreach (var room in root.GetRooms())
        {
            for (var x = room.xMin + 1; x < room.xMax - 1; x++)
            {
                for (var y = room.yMin + 1; y < room.yMax - 1; y++)
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
                    var obj = floor.pool.GetObject();
                    obj.parent = parent;
                    obj.position = pos;
                    floorPosData.Add(pos);
                }

                if (m_tiles.TryGetValue(TileType.Celling, out var ceiling))
                {
                    var obj = ceiling.pool.GetObject();
                    obj.position = pos + ceiling.offset;
                    obj.parent = parent;
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
                    var obj = wall.pool.GetObject();
                    obj.position = pos + wall.offset;
                    obj.parent = parent;
                }

                mapData[x, y] = TileType.Wall;
            }
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                CreatePillar(mapData, x, y, ToWorldPosition(bounds, x, y, blockSize), parent);
            }
        }
    }

    private Vector3 ToWorldPosition(RectInt bounds, int x, int y, Vector3Int blockSize)
    {
        return new Vector3(bounds.x + x * blockSize.x, 0, bounds.y + y * blockSize.z);
    }

    private bool IsInside(TileType[,] map, int x, int y)
    {
        return x >= 0 && y >= 0 && x < map.GetLength(0) && y < map.GetLength(1);
    }

    private void CreatePillar(TileType[,] map, int x, int y, Vector3 wallPos, Transform parent)
    {
        if (x < 1 || y > map.GetLength(1) - 2) return;

        var left = map[x - 1, y] == TileType.Wall;
        var up = map[x, y + 1] == TileType.Wall;
        var leftUp = map[x - 1, y + 1] == TileType.Wall;
        var exceptions = (left && up && leftUp) || (!left && up && !leftUp) || (left && !up && !leftUp);
        var exceptions2 = !left && !up && leftUp;

        var shouldCreate = exceptions ^ (map[x, y] == TileType.Wall);
        shouldCreate |= exceptions2;

        if (shouldCreate && m_tiles.TryGetValue(TileType.Pillar, out var pillar))
        {
            var obj = pillar.pool.GetObject();
            obj.position = wallPos + pillar.offset;
            obj.parent = parent;
        }
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

        var edgeStart = GetChunkEdgeStart(chunk, blockSize, chunkToNeighborDir);

        if (chunkToNeighborDir == Vector2Int.up || chunkToNeighborDir == Vector2Int.down)
        {
            if (TryBreakSlimWall(edgeStart, chunkToNeighborDir, Vector3.right, blockSize, 2, chunk.Bounds.width, chunk.ChunkObject.transform))
            {
                return true;
            }

            return TryCarveFallbackTunnel(chunk, neighborChunk, chunkToNeighborDir, blockSize);
        }

        if (chunkToNeighborDir == Vector2Int.left || chunkToNeighborDir == Vector2Int.right)
        {
            if (TryBreakSlimWall(edgeStart, chunkToNeighborDir, Vector3.forward, blockSize, 2, chunk.Bounds.height, chunk.ChunkObject.transform))
            {
                return true;
            }

            return TryCarveFallbackTunnel(chunk, neighborChunk, chunkToNeighborDir, blockSize);
        }

        return false;
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

        if (dir == Vector2Int.up)
        {
            var maxZ = floors.Max(pos => pos.z);
            return floors.Where(pos => Mathf.Approximately(pos.z, maxZ)).OrderBy(_ => Random.value).First();
        }

        if (dir == Vector2Int.down)
        {
            var minZ = floors.Min(pos => pos.z);
            return floors.Where(pos => Mathf.Approximately(pos.z, minZ)).OrderBy(_ => Random.value).First();
        }

        if (dir == Vector2Int.right)
        {
            var maxX = floors.Max(pos => pos.x);
            return floors.Where(pos => Mathf.Approximately(pos.x, maxX)).OrderBy(_ => Random.value).First();
        }

        var minX = floors.Min(pos => pos.x);
        return floors.Where(pos => Mathf.Approximately(pos.x, minX)).OrderBy(_ => Random.value).First();
    }

    private void CarveGridTunnel(Chunk chunk, Chunk neighborChunk, Vector3 start, Vector3 end, Vector2Int dir, Vector3Int blockSize)
    {
        var current = start;
        var forwardStep = new Vector3(dir.x * blockSize.x, 0f, dir.y * blockSize.z);
        var sideStep = dir.x == 0 ? new Vector3(Mathf.Sign(end.x - start.x) * blockSize.x, 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(end.z - start.z) * blockSize.z);

        CarveTunnelCell(chunk, neighborChunk, current, blockSize);

        if (dir.x == 0)
        {
            while (Mathf.Approximately(current.z, end.z) == false)
            {
                current += forwardStep;
                if ((dir.y > 0 && current.z > end.z) || (dir.y < 0 && current.z < end.z))
                {
                    current.z = end.z;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize);
            }

            while (Mathf.Approximately(current.x, end.x) == false)
            {
                current += sideStep;
                if ((sideStep.x > 0f && current.x > end.x) || (sideStep.x < 0f && current.x < end.x))
                {
                    current.x = end.x;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize);
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

                CarveTunnelCell(chunk, neighborChunk, current, blockSize);
            }

            while (Mathf.Approximately(current.z, end.z) == false)
            {
                current += sideStep;
                if ((sideStep.z > 0f && current.z > end.z) || (sideStep.z < 0f && current.z < end.z))
                {
                    current.z = end.z;
                }

                CarveTunnelCell(chunk, neighborChunk, current, blockSize);
            }
        }
    }

    private void CarveTunnelCell(Chunk chunk, Chunk neighborChunk, Vector3 pos, Vector3Int blockSize)
    {
        var owner = ContainsWorldPosition(chunk, pos, blockSize) ? chunk : neighborChunk;
        RemoveBlockingTiles(pos, blockSize);
        if (HasTileAt(pos, TileType.Floor, blockSize) == false)
        {
            CreateTunnelFloor(pos, owner.ChunkObject.transform);
        }

        owner.floorPosData ??= new List<Vector3>();
        if (owner.floorPosData.Any(floor => Vector3.SqrMagnitude(floor - pos) < 0.01f) == false)
        {
            owner.floorPosData.Add(pos);
        }
    }

    private bool HasTileAt(Vector3 pos, TileType tileType, Vector3Int blockSize)
    {
        var layer = LayerMask.NameToLayer(tileType.ToString());
        if (layer < 0) return false;

        var halfExtents = new Vector3(blockSize.x * 0.45f, 1.5f, blockSize.z * 0.45f);
        return Physics.OverlapBox(pos + Vector3.up * 0.5f, halfExtents, Quaternion.identity, 1 << layer).Length > 0;
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
        var pillarLayer = LayerMask.NameToLayer(TileType.Pillar.ToString());
        var gateLayer = LayerMask.NameToLayer(TileType.Gate.ToString());
        var mask = 0;

        if (wallLayer >= 0) mask |= 1 << wallLayer;
        if (pillarLayer >= 0) mask |= 1 << pillarLayer;
        if (gateLayer >= 0) mask |= 1 << gateLayer;
        if (mask == 0) mask = Physics.DefaultRaycastLayers;

        var halfExtents = new Vector3(blockSize.x * 0.45f, 1.5f, blockSize.z * 0.45f);
        var hits = Physics.OverlapBox(pos + Vector3.up * 0.5f, halfExtents, Quaternion.identity, mask);
        foreach (var hit in hits)
        {
            Destroy(hit.gameObject);
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

        var tunnelList = Enumerable.Range(0, maxLength).OrderBy(_ => Random.value).ToList();
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

        foreach (var wallIndex in tunnelList)
        {
            var startPos = startTunnelOffset + wallDir * blockRightSize * wallIndex;
            var hits = Physics.RaycastAll(startPos, rayDir, rayDistance, wallMask);
            if (hits.Length == 0 || hits.Length > thickness) continue;

            CarveTunnel(startPos, rayDir, rayDistance, hits, parent);
            return true;
        }

        foreach (var wallIndex in tunnelList)
        {
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
        foreach (var hitInfo in hits.OrderBy(hitInfo => hitInfo.distance))
        {
            var pos = new Vector3(hitInfo.transform.position.x, 0, hitInfo.transform.position.z);
            CreateTunnelFloor(pos, parent);
            tunnelLength = Mathf.Max(tunnelLength, hitInfo.distance);
            Destroy(hitInfo.collider.gameObject);
        }

        CreateTunnelGates(startPos, rayDir, tunnelLength, parent);
    }

    private void CreateTunnelFloor(Vector3 pos, Transform parent)
    {
        if (m_tiles.TryGetValue(TileType.Floor, out var floor))
        {
            var obj = floor.pool.GetObject();
            obj.position = pos;
            obj.parent = parent;
        }

        if (m_tiles.TryGetValue(TileType.Celling, out var ceiling))
        {
            var obj = ceiling.pool.GetObject();
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

        var gate1 = gate.pool.GetObject();
        gate1.position = rotatedPos;
        gate1.rotation = rotation;
        gate1.parent = parent;

        var gate2 = gate.pool.GetObject();
        gate2.position = rotatedPos + tunnelLength * rayDir;
        gate2.rotation = rotation;
        gate2.parent = parent;
    }
}
