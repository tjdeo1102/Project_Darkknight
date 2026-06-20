using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class Chunk
{
    public Vector2Int ChunkCoord;
    public Vector3Int BlockSize;
    public GameObject ChunkObject;
    public bool IsLoaded = false;
    public bool IsGenerate = false;
    public bool IsGenerateMonster = false;
    public bool IsCombineMesh = false;
    public bool IsCombiningMesh = false;
    public bool NeedsMeshRebuild = false;
    public bool IsRestArea = false;
    public bool HasRestAreaRoom = false;
    public HashSet<Vector2Int> CheckDirection = new HashSet<Vector2Int>();
    public List<GameObject> CombinedMeshObjects = new List<GameObject>();
    public RectInt Bounds;
    public List<Vector3> floorPosData;
    public List<RectInt> RoomData;
    public RectInt RestAreaRoom;
    public Vector3 RestAreaCenter;
    public readonly List<Vector3> RestAreaFloorPositions = new();
    public readonly List<Vector3> RestAreaInnerFloorPositions = new();
    public readonly List<GameObject> RestAreaLightObjects = new();

    public NavMeshModifier[] navMeshModifiers;

    public Chunk(Vector2Int coord, int size, Vector3Int blockSize, Transform parent)
    {
        ChunkCoord = coord;
        BlockSize = blockSize;
        Bounds = new RectInt(coord.x * size * blockSize.x,
            coord.y * size * blockSize.z,
            size, size);
        ChunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        ChunkObject.transform.parent = parent;

        ChunkObject.transform.position = new Vector3(
            coord.x * size * blockSize.x,
            0f,
            coord.y * size * blockSize.z);

        ChunkObject.SetActive(false);
    }

    public void Load()
    {
        if (IsLoaded) return;
        ChunkObject.SetActive(true);
        IsLoaded = true;
    }

    public void Unload()
    {
        if (!IsLoaded) return;
        IsLoaded = false;
        ChunkObject.SetActive(false);
    }

    public IEnumerator GenerateRoutine(int minRoomSize, Vector3Int blockSize)
    {
        if (IsGenerate) yield break;

        IsGenerate = true;
        yield return ChunkManager.Instance.Generator.GenerateChunkRoutine(
            Bounds,
            ChunkObject.transform,
            minRoomSize,
            blockSize,
            (floors, rooms) =>
            {
                floorPosData = floors;
                RoomData = rooms;
            });
        RefreshNavMeshModifiers();
    }

    public void ClearRestArea()
    {
        IsRestArea = false;
        HasRestAreaRoom = false;
        RestAreaRoom = default;
        RestAreaCenter = default;
        RestAreaFloorPositions.Clear();
        RestAreaInnerFloorPositions.Clear();
    }

    public bool TrySelectRestAreaRoom(int innerPaddingCells)
    {
        if (RoomData == null || RoomData.Count == 0)
        {
            ClearRestArea();
            return false;
        }

        RestAreaRoom = RoomData[Random.Range(0, RoomData.Count)];
        HasRestAreaRoom = true;
        IsRestArea = true;
        RestAreaCenter = new Vector3(
            Bounds.x + RestAreaRoom.center.x * BlockSize.x,
            0f,
            Bounds.y + RestAreaRoom.center.y * BlockSize.z);
        RebuildRestAreaFloorCache(innerPaddingCells);
        return true;
    }

    public bool IsWorldPositionInRestAreaRoom(Vector3 position)
    {
        if (IsRestArea == false || HasRestAreaRoom == false || BlockSize.x <= 0 || BlockSize.z <= 0)
            return false;

        var localX = Mathf.RoundToInt((position.x - Bounds.x) / BlockSize.x);
        var localY = Mathf.RoundToInt((position.z - Bounds.y) / BlockSize.z);
        return RestAreaRoom.Contains(new Vector2Int(localX, localY));
    }

    public IReadOnlyList<Vector3> GetRestAreaFloorPositions(bool preferInnerArea)
    {
        if (IsRestArea == false || HasRestAreaRoom == false)
            return RestAreaFloorPositions;

        if (preferInnerArea && RestAreaInnerFloorPositions.Count > 0)
            return RestAreaInnerFloorPositions;

        return RestAreaFloorPositions;
    }

    private void RebuildRestAreaFloorCache(int innerPaddingCells)
    {
        RestAreaFloorPositions.Clear();
        RestAreaInnerFloorPositions.Clear();
        if (floorPosData == null || IsRestArea == false || HasRestAreaRoom == false)
            return;

        var innerRoom = RestAreaRoom;
        if (innerPaddingCells > 0 &&
            RestAreaRoom.width > innerPaddingCells * 2 &&
            RestAreaRoom.height > innerPaddingCells * 2)
        {
            innerRoom = new RectInt(
                RestAreaRoom.xMin + innerPaddingCells,
                RestAreaRoom.yMin + innerPaddingCells,
                RestAreaRoom.width - innerPaddingCells * 2,
                RestAreaRoom.height - innerPaddingCells * 2);
        }

        foreach (var pos in floorPosData)
        {
            if (IsWorldPositionInRoom(pos, RestAreaRoom))
                RestAreaFloorPositions.Add(pos);
            if (IsWorldPositionInRoom(pos, innerRoom))
                RestAreaInnerFloorPositions.Add(pos);
        }
    }

    private bool IsWorldPositionInRoom(Vector3 position, RectInt room)
    {
        if (BlockSize.x <= 0 || BlockSize.z <= 0)
            return false;

        var localX = Mathf.RoundToInt((position.x - Bounds.x) / BlockSize.x);
        var localY = Mathf.RoundToInt((position.z - Bounds.y) / BlockSize.z);
        return room.Contains(new Vector2Int(localX, localY));
    }

    public void RefreshNavMeshModifiers()
    {
        navMeshModifiers = ChunkObject.transform.GetComponentsInChildren<NavMeshModifier>(true);
    }

    public bool IsCheckClosedChunk(Vector2Int dir)
    {
        if (CheckDirection.Contains(dir) == false)
        {
            return false;
        }
        return true;
    }
}
