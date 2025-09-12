using System.Collections.Generic;
using UnityEngine;

public class DungeonGrid
{
    public readonly int Width;
    public readonly int Height;
    public readonly bool[,] Walkable;
    public readonly List<RectInt> Rooms = new();
    public readonly List<Vector2Int> RoomCenters = new();
    public Vector2Int StartCenterCell;

    public DungeonGrid(int width, int height)
    {
        Width = width; Height = height;
        Walkable = new bool[width, height];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public void CarveRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        for (int y = room.yMin; y < room.yMax; y++)
            Walkable[x, y] = true;
    }

    public void CarveCell(int x, int y, int corridorWidth)
    {
        int half = corridorWidth / 2;
        for (int dx = -half; dx <= half; dx++)
        for (int dy = -half; dy <= half; dy++)
        {
            int nx = x + dx, ny = y + dy;
            if (InBounds(nx, ny)) Walkable[nx, ny] = true;
        }
    }
}
