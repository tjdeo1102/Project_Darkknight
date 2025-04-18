using System.Collections.Generic;
using UnityEngine;

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

    public Chunk(Vector2Int coord, int size)
    {
        ChunkCoord = coord;
        Bounds = new RectInt(coord.x * size, coord.y * size, size, size);
        ChunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        ChunkObject.SetActive(false);
    }

    public void Load()
    {
        ChunkObject.SetActive(true);
        IsLoaded = true;
    }

    public void Unload()
    {
        ChunkObject.SetActive(false);
        IsLoaded = false;
    }

    public void Generate(int minRoomSize)
    {
        if (!IsGenerate)
        {
            IsGenerate = true;
            MapGenerator.Instance.GenerateChunk(Bounds, ChunkObject.transform, minRoomSize, out floorPosData);
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
