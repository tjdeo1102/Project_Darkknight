using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public enum TileType
{
    Empty,
    Floor,
    Wall,
    Pillar,
    Celling,
}

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

    public static MapGenerator Instance;

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

    public void GenerateChunk(RectInt bounds, Transform parent, int minRoomSize, out List<Vector3> floorPosData)
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
                    Vector3 pos = new Vector3(bounds.x + x, 0, bounds.y + y);
                    Transform floor = GameObject.Instantiate(FloorPrefab, pos, Quaternion.identity, parent).transform;
                    floorPosData.Add(floor.position);
                    GameObject.Instantiate(CeilingPrefab, pos + CeilingOffset, Quaternion.identity, parent);
                }
            }
        }

        // 비어있는 구역은 임의의 벽과 땅으로 메우기
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapData[x, y] == TileType.Empty)
                {
                    Vector3 pos = new Vector3(bounds.x + x, 0, bounds.y + y);
                    GameObject.Instantiate(FloorPrefab, pos, Quaternion.identity, parent);
                    GameObject.Instantiate(WallPrefab, pos + WallOffset, Quaternion.identity, parent);
                    mapData[x, y] = TileType.Wall;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(bounds.x + x, 0, bounds.y + y);
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
        if (res)
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

    public bool TryConnectChunks(RectInt a, RectInt b, Transform parent)
    {
        var res = false;
        // 두 청크가 수평/수직으로 인접한 경우에만 연결
        if (a.Overlaps(b)) return res;

        // 가장자리 중 가까운 위치를 반환
        // 오른쪽
        if (a.xMax == b.xMin)
        {
            int z = (int)Mathf.Clamp(b.center.y, a.yMin + 1, a.yMax - 2);
            return TryBreakSlimWall(new Vector2Int(a.xMax - 1, z), Vector2Int.right, Vector2Int.up, 2, a.height);
        }
        // 왼쪽
        if (a.xMin == b.xMax)
        {
            int z = (int)Mathf.Clamp(b.center.y, a.yMin + 1, a.yMax - 2);
            return TryBreakSlimWall(new Vector2Int(a.xMin, z), Vector2Int.left, Vector2Int.up, 2, a.height);
        }
        // 위쪽
        if (a.yMax == b.yMin)
        {
            int x = (int)Mathf.Clamp(b.center.x, a.xMin + 1, a.xMax - 2);
            return TryBreakSlimWall(new Vector2Int(x, a.yMax - 1), Vector2Int.up, Vector2Int.right, 2, a.width);
        }
        // 아래쪽
        if (a.yMin == b.yMax)
        {
            int x = (int)Mathf.Clamp(b.center.x, a.xMin + 1, a.xMax - 2);
            return TryBreakSlimWall(new Vector2Int(x, a.yMin), Vector2Int.down, Vector2Int.right, 2, a.width);
        }

        return res;
    }

    private bool TryBreakSlimWall(Vector2Int edge, Vector2Int turnelDir, Vector2Int wallDir, int offset, int maxLength)
    {
        // 중앙부터 멀어지는 방향대로 벽 가장자리를 따라 조건에 부합하는 가장자리 좌표 확인
        // 일정 오프셋 뒤에서 터널 뚫는 방향으로 RaycastAll로 벽의 개수 검사
        for (int i = 0; i < maxLength / 2; i++)
        {
            // Cast시작할 좌표 (중앙에서 부터 멀어지는 방향으로 탐색)
            for (int j = 0; j < 2; j++)
            {
                var a = edge - offset * turnelDir + i * wallDir * (int)Mathf.Pow(-1,j);
                var startPos = new Vector3(a.x, 1, a.y);
                var rayDir = new Vector3(turnelDir.x, 0, turnelDir.y);
                var res = Physics.RaycastAll(startPos, rayDir, offset * 2 + 1, 1 << LayerMask.NameToLayer("Wall"));
                // 벽이 2개인 경우에만 터널 뚫기
                if (res.Length == 2)
                {
                    //Debug.DrawRay(startPos, new Vector3(turnelDir.x, 0, turnelDir.y) * (offset * 2 + 1), Color.red, 3000f);

                    foreach (var obj in res)
                    {
                        Destroy(obj.collider.gameObject);
                    }
                    // 없어진 자리에는 아치형 문 생성
                    // 회전 및 축 보정
                    Quaternion rotation = Quaternion.LookRotation(rayDir);
                    Vector3 rotatedOffset = rotation * GateOffset;
                    GameObject.Instantiate(GatePrefab, startPos + rotatedOffset, rotation);
                    GameObject.Instantiate(GatePrefab, startPos + rotatedOffset + rayDir * 2f, rotation);
                    return true;
                }
            }
        }
        return false;
    }


}
