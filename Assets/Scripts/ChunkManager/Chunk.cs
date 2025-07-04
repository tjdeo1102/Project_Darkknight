using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Chunk
{
    public Vector2Int ChunkCoord;
    public GameObject ChunkObject;
    public bool IsLoaded = false;
    public bool IsGenerate = false;
    public bool IsCombineMesh = false;
    public HashSet<Vector2> CheckDirection = new HashSet<Vector2>();
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

    public void Generate(int minRoomSize, Vector3Int blockSize)
    {
        if (!IsGenerate)
        {
            IsGenerate = true;
            ChunkManager.Instance.Generator.GenerateChunk(Bounds, ChunkObject.transform, minRoomSize, blockSize, out floorPosData);
            navMeshModifiers = ChunkObject.transform.GetComponentsInChildren<NavMeshModifier>(true);
        }
    }

    public bool IsCheckClosedChunk(Vector2 dir)
    {
        if (CheckDirection.Contains(dir) == false)
        {
            return false;
        }
        return true;
    }
}
