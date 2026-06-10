using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class Chunk
{
    public Vector2Int ChunkCoord;
    public GameObject ChunkObject;
    public bool IsLoaded = false;
    public bool IsGenerate = false;
    public bool IsGenerateMonster = false;
    public bool IsCombineMesh = false;
    public bool IsCombiningMesh = false;
    public bool NeedsMeshRebuild = false;
    public HashSet<Vector2Int> CheckDirection = new HashSet<Vector2Int>();
    public List<GameObject> CombinedMeshObjects = new List<GameObject>();
    public RectInt Bounds;
    public List<Vector3> floorPosData;

    public NavMeshModifier[] navMeshModifiers;

    public Chunk(Vector2Int coord, int size, Vector3Int blockSize, Transform parent)
    {
        ChunkCoord = coord;
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
            result => floorPosData = result);
        RefreshNavMeshModifiers();
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
