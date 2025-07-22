using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    public RectInt Area;
    public BSPNode Left, Right;
    public bool IsLeaf => Left == null && Right == null;
    private RectInt room;
    public RectInt Room => room;

    public BSPNode(RectInt area)
    {
        Area = area;
    }

    public void Split(int minSize)
    {
        if (!IsLeaf) return;

        bool splitHorizontally;

        if (Area.width > Area.height && Area.width >= 2 * minSize)
            splitHorizontally = true;
        else if (Area.height >= 2 * minSize)
            splitHorizontally = false;
        else
            return; // 더 이상 분할 불가

        if (splitHorizontally)
        {
            int splitMin = Mathf.FloorToInt(Area.width * 0.3f);
            int splitMax = Mathf.FloorToInt(Area.width * 0.8f);
            if (splitMax <= minSize) return;

            int splitX = Random.Range(splitMin, splitMax);
            Left = new BSPNode(new RectInt(Area.x, Area.y, splitX, Area.height));
            Right = new BSPNode(new RectInt(Area.x + splitX, Area.y, Area.width - splitX, Area.height));
        }
        else
        {
            int splitMin = Mathf.FloorToInt(Area.height * 0.3f);
            int splitMax = Mathf.FloorToInt(Area.height * 0.8f);
            if (splitMax <= minSize) return;

            int splitY = Random.Range(splitMin, splitMax);
            Left = new BSPNode(new RectInt(Area.x, Area.y, Area.width, splitY));
            Right = new BSPNode(new RectInt(Area.x, Area.y + splitY, Area.width, Area.height - splitY));
        }

        Left.Split(minSize);
        Right.Split(minSize);

        // Leaf 노드에 방 생성
        if (Left.IsLeaf) Left.CreateRoom();
        if (Right.IsLeaf) Right.CreateRoom();
    }

    public void CreateRoom(int margin = 1)
    {
        int roomWidth = Random.Range(Area.width / 2, Area.width - margin);
        int roomHeight = Random.Range(Area.height / 2, Area.height - margin);
        int roomX = Area.x + Random.Range(1, Area.width - roomWidth - 1);
        int roomY = Area.y + Random.Range(1, Area.height - roomHeight - 1);

        room = new RectInt(roomX, roomY, roomWidth, roomHeight);
    }



    public List<RectInt> GetRooms()
    {
        List<RectInt> rooms = new List<RectInt>();
        if (IsLeaf)
        {
            rooms.Add(Area);
        }
        else
        {
            rooms.AddRange(Left.GetRooms());
            rooms.AddRange(Right.GetRooms());
        }
        return rooms;
    }

    public Vector2Int GetRoomCenter()
    {
        if (room.width == 0 || room.height == 0)
            return new Vector2Int(Area.x + Area.width / 2, Area.y + Area.height / 2);
        return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
    }

}
