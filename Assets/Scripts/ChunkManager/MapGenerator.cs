using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;


public class MapGenerator: MonoBehaviour
{
    public GameObject FloorPrefab;
    public GameObject WallPrefab;
    public GameObject PillarPrefab;
    public GameObject CeilingPrefab;
    public GameObject GatePrefab;

    public Vector3 WallOffset;
    public Vector3 PillarOffset;
    public Vector3 CeilingOffset;
    public Vector3 GateOffset;

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
                    if (FloorPrefab != null)
                    {
                        Transform floor = GameObject.Instantiate(FloorPrefab, pos, Quaternion.identity, parent).transform;
                        floorPosData.Add(floor.position);
                    }
                    if (CeilingPrefab != null) GameObject.Instantiate(CeilingPrefab, pos + CeilingOffset, Quaternion.identity, parent);
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
                    if (WallPrefab != null)
                    {
                        GameObject.Instantiate(WallPrefab, pos + WallOffset, Quaternion.identity, parent);
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
        if (res && PillarPrefab != null)
        {
            var pillarPos = wallPos + PillarOffset;
            GameObject.Instantiate(PillarPrefab, pillarPos, Quaternion.identity, parent);
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

    public bool TryConnectChunks(RectInt a, RectInt b,Vector3Int blockSize, Transform parent)
    {
        var res = false;
        // 이미 들어오는 인자는 인접한 청크임을 보장

        // 가장자리 중 가까운 위치를 반환, blockSize 고려 좌표로 재계산
        // 오른쪽
        if (a.xMin + blockSize.x * a.width == b.xMin)
        {
            int z = (int)Mathf.Clamp(b.center.y, a.yMin + 1, a.yMax - 2) * blockSize.z;
            return TryBreakSlimWall(new Vector2Int((a.xMax - 1) * blockSize.x, z), Vector2Int.right, blockSize, 2, a.height, parent);
        }
        // 왼쪽
        if (a.xMin == b.xMin + blockSize.x * b.width)
        {
            int z = (int)Mathf.Clamp(b.center.y, a.yMin + 1, a.yMax - 2) * blockSize.z;
            return TryBreakSlimWall(new Vector2Int(a.xMin, z), Vector2Int.left, blockSize, 2, a.height, parent);
        }
        // 위쪽
        if (a.yMin + blockSize.z * a.height == b.yMin)
        {
            int x = (int)Mathf.Clamp(b.center.x, a.xMin + 1, a.xMax - 2) * blockSize.x;
            return TryBreakSlimWall(new Vector2Int(x, (a.yMax - 1) * blockSize.z), Vector2Int.up, blockSize, 2, a.width, parent);
        }
        // 아래쪽
        if (a.yMin == b.yMin + blockSize.z * b.height)
        {
            int x = (int)Mathf.Clamp(b.center.x, a.xMin + 1, a.xMax - 2) * blockSize.x;
            return TryBreakSlimWall(new Vector2Int(x, a.yMin), Vector2Int.down, blockSize, 2, a.width, parent);
        }

        if (res == false)
        {
            print($"실패 {a} {b} {blockSize}");
        }
        return res;
    }

    private bool TryBreakSlimWall(Vector2Int edge, Vector2Int turnelDir,Vector3Int blockSize, int offset, int maxLength, Transform parent)
    {
        Vector2Int wallDir;
        int wallSize;
        int offsetSize;
        if (turnelDir == Vector2Int.right || turnelDir == Vector2Int.left)
        {
            wallDir = Vector2Int.up;
            wallSize = blockSize.z;
            offsetSize = blockSize.x;
        }
        else
        {
            wallDir = Vector2Int.right;
            wallSize = blockSize.x;
            offsetSize = blockSize.z;
        }

        List<int> turnelList = Enumerable.Range(0, (int) maxLength / 2).ToList();
        turnelList = turnelList.OrderBy(x => Random.value).ToList();

        // 랜덤으로 벽 가장자리를 따라 조건에 부합하는 가장자리 좌표 확인
        // 일정 오프셋 뒤에서 터널 뚫는 방향으로 RaycastAll로 벽의 개수 검사
        foreach (var item in turnelList)
        {
            // Cast시작할 좌표 (중앙에서 부터 멀어지는 방향으로 탐색)
            for (int j = 0; j < 2; j++)
            {
                var a = edge - offset * offsetSize * turnelDir + item * wallSize * wallDir * (int)Mathf.Pow(-1,j);
                // TODO: 블럭 사이즈 변수 고려한 위치 추가
                var startPos = new Vector3(a.x, 1, a.y);
                var rayDir = new Vector3(turnelDir.x, 0, turnelDir.y);
                var res = Physics.RaycastAll(startPos, rayDir, (offset * 2 + 1) * offsetSize, 1 << LayerMask.NameToLayer("Wall"));
                // 벽이 2개인 경우에만 터널 뚫기
                if (res.Length == 2)
                {
                    //Debug.DrawRay(startPos, new Vector3(turnelDir.x, 0, turnelDir.y) * (offset * 2 + 1) * offsetSize, Color.red, 3000f);

                    foreach (var obj in res)
                    {
                        var pos = new Vector3(obj.transform.position.x, 0 , obj.transform.position.z) ;
                        if (CeilingPrefab != null)
                        {
                            GameObject.Instantiate(CeilingPrefab, pos+ CeilingOffset, Quaternion.identity, parent);
                        }
                        if (FloorPrefab != null)
                        {
                            GameObject.Instantiate(FloorPrefab, pos, Quaternion.identity, parent);
                        }
                        Destroy(obj.collider.gameObject);

                    }
                    // 없어진 자리에는 아치형 문 생성
                    // 회전 및 축 보정
                    Quaternion rotation = Quaternion.LookRotation(rayDir);
                    Vector3 forward = rayDir;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    Vector3 up = Vector3.up;

                    // forward각이 서로 90차이나는 경우, right의 방향을 서로 뒤집어야 함
                    // 다른 그렇지 않은 경우에 서로 x 오프셋 방향 반대로 작용
                    if (Mathf.Abs(forward.x) > 0.5f && Mathf.Abs(forward.z) < 0.5f)
                    {
                        right = -right;
                    }

                    Vector3 rotatedPos = startPos
                                       + GateOffset.z * forward
                                       + GateOffset.x * right
                                       + GateOffset.y * up;

                    if (GatePrefab != null)
                    {
                        GameObject.Instantiate(GatePrefab, rotatedPos, rotation,parent);
                        GameObject.Instantiate(GatePrefab, rotatedPos + 2 * offsetSize * rayDir, rotation, parent);
                    }
                    //Debug.DrawRay(rotatedPos, forward * 2f, Color.red, 1000f);
                    //Debug.DrawRay(rotatedPos, right * 2f, Color.green, 1000f);   // x방향
                    return true;
                }
            }
        }
        return false;
    }


}
