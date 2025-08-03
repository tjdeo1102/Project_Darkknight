using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public class MapGenerator: MonoBehaviour
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
        m_tiles = new();
        foreach (var t in Tiles)
        {
            t.pool.Init();
            m_tiles[t.type] = t;
            StartCoroutine(t.pool.AutoCreateObjectPerFrame());
        }
    }

    public void GenerateChunk(RectInt bounds, Transform parent, int minRoomSize, Vector3Int blockSize, out List<Vector3> floorPosData)
    {
        int width = bounds.width;
        int height = bounds.height;
        var mapData = new TileType[width, height];
        floorPosData = new List<Vector3>();

        BSPNode root = new BSPNode(new RectInt(0, 0, width, height));
        root.Split(minRoomSize);

        List<RectInt> rooms = root.GetRooms();

        // 방 생성
        foreach (var room in rooms)
        {
            for (int x = room.xMin + 1; x < room.xMax - 1; x++)
            {
                for (int y = room.yMin + 1; y < room.yMax - 1; y++)
                {
                    mapData[x, y] = TileType.Floor;
                }
            }
        }

        // 복도 연결
        ConnectRooms(root, mapData);


        // 바닥 생성
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == TileType.Floor)
                {
                    Vector3 pos = new Vector3(bounds.x + (x*blockSize.x), 0, bounds.y + (y*blockSize.z));
                    if (m_tiles.TryGetValue(TileType.Floor, out var floor))
                    {
                        Transform obj = floor.pool.GetObject();
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
        }

        // 비어있는 구역은 임의의 벽으로 메우기
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == TileType.Empty)
                {
                    Vector3 pos = new Vector3(bounds.x + (x * blockSize.x), 0, bounds.y + (y * blockSize.z));
                    if (m_tiles.TryGetValue(TileType.Wall, out var wall))
                    {
                        var obj = wall.pool.GetObject();
                        obj.position = pos + wall.offset;
                        obj.parent = parent;
                    }
                    mapData[x, y] = TileType.Wall;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(bounds.x + (x * blockSize.x), 0, bounds.y + (y * blockSize.z));
                CreatePillar(mapData, x, y, pos, parent);
            }
        }
    }


    private void CreatePillar(TileType[,] map, int x, int y, Vector3 wallPos, Transform parent)
    {
        // 1x1부터 좌상단 4개의 블럭을 검사하도록
        if (x < 1 || y > map.GetLength(1) - 2) return;

        // 규칙: [좌,상,좌상] , [상], [좌]에 블럭이 있는 경우를 제외하면 좌상단 모서리에 기둥 필요
        // 예외: x,y에 벽이 없는 경우는 위의 경우에서 기둥 필요
        // 예외2: 좌상단만 벽이 있는 경우, x,y의 벽 유무와 상관없이 기둥 필요
        bool left = map[x-1, y] == TileType.Wall;
        bool up = map[x, y + 1] == TileType.Wall;
        bool leftUp = map[x - 1, y + 1] == TileType.Wall;
        bool exceptions = (left && up && leftUp) || (!left && up && !leftUp) || (left && !up && !leftUp);
        bool exceptions2 = !left && !up && leftUp;

        bool res = exceptions ^ (map[x, y] == TileType.Wall);
        res |= exceptions2;
        if (res && m_tiles.TryGetValue(TileType.Pillar, out var pillar))
        {
            var obj = pillar.pool.GetObject();
            obj.position = wallPos + pillar.offset;
            obj.parent = parent;
        }
    }

    private void ConnectRooms(BSPNode node, TileType[,] mapData)
    {
        if (node.IsLeaf) return;

        Vector2Int centerA = node.Left.GetRoomCenter();
        Vector2Int centerB = node.Right.GetRoomCenter();

        if (Random.value > 0.5f)
        {
            for (int x = Mathf.Min(centerA.x, centerB.x); x <= Mathf.Max(centerA.x, centerB.x); x++)
                mapData[x, centerA.y] = TileType.Floor;
            for (int y = Mathf.Min(centerA.y, centerB.y); y <= Mathf.Max(centerA.y, centerB.y); y++)
                mapData[centerB.x, y] = TileType.Floor;
        }
        else
        {
            for (int y = Mathf.Min(centerA.y, centerB.y); y <= Mathf.Max(centerA.y, centerB.y); y++)
                mapData[centerA.x, y] = TileType.Floor;
            for (int x = Mathf.Min(centerA.x, centerB.x); x <= Mathf.Max(centerA.x, centerB.x); x++)
                mapData[x, centerB.y] = TileType.Floor;
        }

        ConnectRooms(node.Left, mapData);
        ConnectRooms(node.Right, mapData);
    }

    public bool TryConnectChunks(Chunk a, Vector2Int aTobDir, Vector3Int blockSize, Transform parent)
    {
        // 이미 들어오는 인자는 인접한 청크임을 보장 + 인접한 면의 길이는 동일
        var midEdge = GetMinEdge(a.Bounds, blockSize, aTobDir);
        // 가장자리의 중심좌표를 미리 구해서 전달
        if (aTobDir == Vector2Int.up || aTobDir == Vector2Int.down)
        {
            return TryBreakSlimWall(midEdge ,aTobDir, Vector3.right, blockSize, 2 , a.Bounds.width, parent);
        }
        else if (aTobDir == Vector2Int.left || aTobDir == Vector2Int.right)
        {
            return TryBreakSlimWall(midEdge, aTobDir, Vector3.forward, blockSize, 2, a.Bounds.height, parent);
        }
        else
        {
            return false;
        }
    }

    private Vector2 GetMinEdge(RectInt bound, Vector3Int blockSize ,Vector2Int turnelDir)
    {
        // 벽 방향은 Right or Up
        if (turnelDir == Vector2Int.up)
        {
            return new Vector2(bound.xMin, bound.yMin * blockSize.x);
        }
        else if (turnelDir == Vector2Int.down)
        {
            return new Vector2(bound.xMin, bound.yMin);
        }
        else if (turnelDir == Vector2Int.left)
        {
            return new Vector2(bound.xMin, bound.yMin);
        }
        else if (turnelDir == Vector2Int.right)
        {
            return new Vector2(bound.xMin * blockSize.z , bound.yMin);
        }
        else
        {
            return Vector2Int.zero;
        }
    }

    private bool TryBreakSlimWall(Vector2 minEdge,Vector2Int turnelDir, Vector3 wallDir, Vector3Int blockSize, int thickness, int maxLength,Transform parent)
    {
        List<int> turnelList = Enumerable.Range(0, maxLength).ToList();
        turnelList = turnelList.OrderBy(x => Random.value).ToList();

        float blockFrontSize = 0;
        float blockRightSize = 0;

        if (wallDir == Vector3.right)
        {
            blockFrontSize = blockSize.z;
            blockRightSize = blockSize.x;
        }
        else
        {
            blockFrontSize = blockSize.x;
            blockRightSize = blockSize.z;
        }
        
        var startXY = minEdge - turnelDir * thickness;
        var startTurnelOffset = new Vector3(startXY.x, 0 ,startXY.y);
        var rayDir = new Vector3(turnelDir.x, 0, turnelDir.y);
        if (m_tiles.TryGetValue(TileType.Wall, out var wall))
            startTurnelOffset += wall.offset.y * Vector3.up;

        // 벽 위치에 맞게 시작위치 보정
        if (Physics.Raycast(startTurnelOffset, rayDir, out var hit, (thickness * 2 + 1) * blockFrontSize, 1 << LayerMask.NameToLayer("Wall")))
            startTurnelOffset = hit.transform.position - rayDir * thickness * blockFrontSize;

        // 랜덤으로 벽 가장자리를 따라 조건에 부합하는 가장자리 좌표 확인
        // 일정 오프셋 뒤에서 터널 뚫는 방향으로 RaycastAll로 벽의 개수 검사
        foreach (var wall_Idx in turnelList)
        {
            var startPos = startTurnelOffset + wallDir * blockRightSize * wall_Idx;
            var res = Physics.RaycastAll(startPos, rayDir, (thickness * 2 + 1) * blockFrontSize, 1 << LayerMask.NameToLayer("Wall"));
            // 벽이 일정두께 이하인 경우에만 터널 뚫기
            if (res.Length <= thickness)
            {
                Debug.DrawRay(startPos, rayDir * (thickness * 2 + 1) * blockFrontSize, Color.red, 3000f);

                foreach (var obj in res)
                {
                    var pos = new Vector3(obj.transform.position.x, 0 , obj.transform.position.z);
                    if (m_tiles.TryGetValue(TileType.Floor, out var floor))
                    {
                        var obj2 = floor.pool.GetObject();
                        obj2.position = pos;
                        obj2.parent = parent;
                    }
                    if (m_tiles.TryGetValue(TileType.Celling, out var ceiling))
                    {
                        var obj2 = ceiling.pool.GetObject();
                        obj2.position = pos + ceiling.offset;
                        obj2.parent = parent;
                    }
                    Destroy(obj.collider.gameObject);
                }
                // 없어진 자리에는 아치형 문 생성
                // 회전 및 축 보정
                if (m_tiles.TryGetValue(TileType.Gate, out var gate))
                {
                    Vector3 forward = rayDir;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    Vector3 up = Vector3.up;

                    //// forward각이 서로 90차이나는 경우, right의 방향을 서로 뒤집어야 함
                    //// 다른 그렇지 않은 경우에 서로 x 오프셋 방향 반대로 작용
                    //if (Mathf.Abs(forward.x) > 0.5f && Mathf.Abs(forward.z) < 0.5f)
                    //{
                    //    right = -right;
                    //}

                    var look = new Vector3(rayDir.x, startPos.y + gate.offset.y, rayDir.z);
                    Quaternion rotation = Quaternion.LookRotation(rayDir);

                    var GateOffset = gate.offset;
                    Vector3 rotatedPos = startPos
                                        + GateOffset.z * forward
                                        + GateOffset.x * right
                                        + GateOffset.y * up;

                    var gate1 = gate.pool.GetObject();
                    gate1.position = rotatedPos;
                    gate1.rotation = rotation;
                    gate1.parent = parent;

                    var gate2 = gate.pool.GetObject();
                    gate2.position = rotatedPos + res.Length * blockFrontSize * rayDir;
                    gate2.rotation = rotation;
                    gate2.parent = parent;

                    //Debug.DrawRay(rotatedPos, forward * 2f, Color.red, 1000f);
                    //Debug.DrawRay(rotatedPos, right * 2f, Color.green, 1000f);   // x방향
                }
                
                return true;
            }
        }
        return false;
    }
}
