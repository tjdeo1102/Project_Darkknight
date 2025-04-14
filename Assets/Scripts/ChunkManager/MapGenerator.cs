using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class MapGenerator
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall
    }

    public static void GenerateChunk(RectInt bounds, Transform parent, GameObject floorPrefab, GameObject wallPrefab, int minRoomSize)
    {
        int width = bounds.width;
        int height = bounds.height;
        TileType[,] mapData = new TileType[width, height];

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
                    Vector3 pos = new Vector3(bounds.x + x, 0, bounds.y + y);
                    GameObject floor = GameObject.Instantiate(floorPrefab, pos, Quaternion.identity, parent);
                }
            }
        }

        //Vector2Int[] directions = {
        //    new Vector2Int(1, 0), new Vector2Int(-1, 0),
        //    new Vector2Int(0, 1), new Vector2Int(0, -1)
        //};

        //for (int x = 1; x < width - 1; x++)
        //{
        //    for (int y = 1; y < height - 1; y++)
        //    {
        //        if (mapData[x, y] == TileType.Floor)
        //        {
        //            foreach (var dir in directions)
        //            {
        //                int nx = x + dir.x;
        //                int ny = y + dir.y;

        //                if (mapData[nx, ny] == TileType.Empty)
        //                {
        //                    mapData[nx, ny] = TileType.Wall;
        //                    Vector3 pos = new Vector3(bounds.x + nx, 1, bounds.y + ny);
        //                    GameObject.Instantiate(wallPrefab, pos, Quaternion.identity, parent);
        //                }
        //            }
        //        }
        //    }
        //}

        // 비어있는 구역은 임의의 벽과 땅으로 메우기
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == TileType.Empty)
                {
                    Vector3 pos = new Vector3(bounds.x + x, 0, bounds.y + y);
                    GameObject.Instantiate(floorPrefab, pos, Quaternion.identity, parent);
                    var wallY = Vector3.up * (wallPrefab.transform.localScale.y / 2 + floorPrefab.transform.localScale.y / 2);
                    GameObject.Instantiate(wallPrefab, pos + wallY, Quaternion.identity, parent);
                }
            }
        }

    }

    private static void ConnectRooms(BSPNode node, TileType[,] mapData)
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

    public static void ConnectChunks(RectInt a, RectInt b, Transform parent, GameObject floorPrefab)
    {
        // 두 청크가 수평/수직으로 인접한 경우에만 연결
        if (a.Overlaps(b)) return;
        Vector2Int pointA = GetEdgeConnectionPoint(a, b);
        Vector2Int pointB = GetEdgeConnectionPoint(b, a);

        // 직선 통로
        if (pointA.x == pointB.x)
        {
            for (int y = Mathf.Min(pointA.y, pointB.y); y <= Mathf.Max(pointA.y, pointB.y); y++)
            {
                Vector3 pos = new Vector3(pointA.x, 0, y);

                if (Physics.Raycast(pos, Vector3.up, out var hit, 10, 1 << LayerMask.NameToLayer("Wall")))
                {
                    // 위쪽의 벽 허물기
                    GameObject.Destroy(hit.collider.gameObject);
                }
            }
        }
        else if (pointA.y == pointB.y)
        {
            for (int x = Mathf.Min(pointA.x, pointB.x); x <= Mathf.Max(pointA.x, pointB.x); x++)
            {
                Vector3 pos = new Vector3(x, 0, pointA.y);
                if (Physics.Raycast(pos, Vector3.up, out var hit, 10, 1 << LayerMask.NameToLayer("Wall")))
                {
                    // 위쪽의 벽 허물기
                    GameObject.Destroy(hit.collider.gameObject);
                }
            }
        }
    }

    private static Vector2Int GetEdgeConnectionPoint(RectInt from, RectInt to)
    {
        // 가장자리 중 가까운 위치를 반환
        // 오른쪽
        if (from.xMax == to.xMin)
        {
            int z = (int)Mathf.Clamp(to.center.y, from.yMin + 1, from.yMax - 2);
            return GetSlimWallPoint(new Vector2Int(from.xMax - 1, z), Vector2Int.right, Vector2Int.up, 2, from.height);
        }
        // 왼쪽
        if (from.xMin == to.xMax)
        {
            int z = (int)Mathf.Clamp(to.center.y, from.yMin + 1, from.yMax - 2);
            return GetSlimWallPoint(new Vector2Int(from.xMin, z), Vector2Int.left, Vector2Int.up, 2, from.height);
        }
        // 위쪽
        if (from.yMax == to.yMin)
        {
            int x = (int)Mathf.Clamp(to.center.x, from.xMin + 1, from.xMax - 2);
            return GetSlimWallPoint(new Vector2Int(x, from.yMax - 1), Vector2Int.up, Vector2Int.right, 2, from.width);
        }
        // 아래쪽
        if (from.yMin == to.yMax)
        {
            int x = (int)Mathf.Clamp(to.center.x, from.xMin + 1, from.xMax - 2);
            return GetSlimWallPoint(new Vector2Int(x, from.yMin), Vector2Int.down, Vector2Int.right, 2, from.width);
        }

        return Vector2Int.RoundToInt(from.center);
    }

    private static Vector2Int GetSlimWallPoint(Vector2Int edge, Vector2Int turnelDir, Vector2Int wallDir, int offset, int maxLength)
    {
        // 중앙부터 멀어지는 방향대로 벽 가장자리를 따라 조건에 부합하는 가장자리 좌표 확인
        // 일정 오프셋 뒤에서 터널 뚫는 방향으로 RaycastAll로 벽의 개수 검사
        for (int i = 0; i < maxLength / 2; i++)
        {
            // Cast시작할 좌표 (중앙에서 부터 멀어지는 방향으로 탐색)
            var a = edge - offset * turnelDir + i * wallDir;
            var startPos = new Vector3(a.x, 1, a.y);
            var res = Physics.RaycastAll(startPos, new Vector3(turnelDir.x, 0, turnelDir.y), offset * 2 + 1, 1 << LayerMask.NameToLayer("Wall"));
            // 벽이 2개인 경우에만 터널 뚫기
            if (res.Length == 2) return edge + i * wallDir;

            // 반대 방향
            var b = edge - offset * turnelDir - i * wallDir;
            var startPos2 = new Vector3(b.x, 1, b.y);
            var res2 = Physics.RaycastAll(startPos, new Vector3(turnelDir.x, 0, turnelDir.y), offset * 2 + 1, 1 << LayerMask.NameToLayer("Wall"));
            // 벽이 2개인 경우에만 터널 뚫기
            if (res.Length == 2) return edge - i * wallDir;

            //Debug.DrawRay(startPos, new Vector3(turnelDir.x, 0, turnelDir.y) * (offset * 2 + 1), Color.red, 3000f);
            //Debug.DrawRay(startPos2, new Vector3(turnelDir.x, 0, turnelDir.y) * (offset * 2 + 1), Color.blue, 3000f);
        }

        return edge;
    }
}
