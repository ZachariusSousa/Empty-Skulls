using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeons/Modules/Tile Painter 2D - Simple", fileName = "SimpleTilePainter2D")]
public class SimpleTilePainter2D : ScriptableObject, ITilePainter2D
{
    public void Paint(DungeonConfig cfg, DungeonTheme2D theme, DungeonGrid grid,
                      Tilemap ground, Tilemap walls, Vector3Int paintOffset)
    {
        ground.ClearAllTiles();
        walls.ClearAllTiles();

        for (int x = 0; x < cfg.width; x++)
        for (int y = 0; y < cfg.height; y++)
            if (grid.Walkable[x, y])
                ground.SetTile(new Vector3Int(x, y, 0) + paintOffset, theme.floorTile);

        for (int x = 0; x < cfg.width; x++)
        for (int y = 0; y < cfg.height; y++)
        {
            if (grid.Walkable[x, y]) continue;
            if (HasWalkableNeighbor(grid, x, y))
                walls.SetTile(new Vector3Int(x, y, 0) + paintOffset, theme.wallTile);
        }
    }

    private bool HasWalkableNeighbor(DungeonGrid grid, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (grid.InBounds(nx, ny) && grid.Walkable[nx, ny]) return true;
        }
        return false;
    }
}
