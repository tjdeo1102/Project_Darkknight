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
        {
            splitHorizontally = true;
        }
        else if (Area.height >= 2 * minSize)
        {
            splitHorizontally = false;
        }
        else
        {
            CreateRoom();
            return;
        }

        if (splitHorizontally)
        {
            var splitMin = Mathf.FloorToInt(Area.width * 0.3f);
            var splitMax = Mathf.FloorToInt(Area.width * 0.8f);
            if (splitMax <= minSize)
            {
                CreateRoom();
                return;
            }

            var splitX = Random.Range(splitMin, splitMax);
            Left = new BSPNode(new RectInt(Area.x, Area.y, splitX, Area.height));
            Right = new BSPNode(new RectInt(Area.x + splitX, Area.y, Area.width - splitX, Area.height));
        }
        else
        {
            var splitMin = Mathf.FloorToInt(Area.height * 0.3f);
            var splitMax = Mathf.FloorToInt(Area.height * 0.8f);
            if (splitMax <= minSize)
            {
                CreateRoom();
                return;
            }

            var splitY = Random.Range(splitMin, splitMax);
            Left = new BSPNode(new RectInt(Area.x, Area.y, Area.width, splitY));
            Right = new BSPNode(new RectInt(Area.x, Area.y + splitY, Area.width, Area.height - splitY));
        }

        Left.Split(minSize);
        Right.Split(minSize);
    }

    public void CreateRoom(int margin = 1)
    {
        if (Area.width <= margin * 2 + 2 || Area.height <= margin * 2 + 2)
        {
            room = Area;
            return;
        }

        var roomWidthMin = Mathf.Max(1, Area.width / 2);
        var roomHeightMin = Mathf.Max(1, Area.height / 2);
        var roomWidthMax = Mathf.Max(roomWidthMin + 1, Area.width - margin);
        var roomHeightMax = Mathf.Max(roomHeightMin + 1, Area.height - margin);

        var roomWidth = Random.Range(roomWidthMin, roomWidthMax);
        var roomHeight = Random.Range(roomHeightMin, roomHeightMax);
        var roomXMax = Mathf.Max(margin + 1, Area.width - roomWidth - margin);
        var roomYMax = Mathf.Max(margin + 1, Area.height - roomHeight - margin);
        var roomX = Area.x + Random.Range(margin, roomXMax);
        var roomY = Area.y + Random.Range(margin, roomYMax);

        room = new RectInt(roomX, roomY, roomWidth, roomHeight);
    }

    public List<RectInt> GetRooms()
    {
        var rooms = new List<RectInt>();
        if (IsLeaf)
        {
            rooms.Add(room.width > 0 && room.height > 0 ? room : Area);
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
        {
            return new Vector2Int(Area.x + Area.width / 2, Area.y + Area.height / 2);
        }

        return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
    }
}
