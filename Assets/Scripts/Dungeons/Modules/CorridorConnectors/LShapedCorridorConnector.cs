using UnityEngine;

[CreateAssetMenu(menuName = "Dungeons/Modules/Corridor Connector - L Shape", fileName = "LShapedCorridorConnector")]
public class LShapedCorridorConnector : ScriptableObject, ICorridorConnector
{
    public void Connect(DungeonConfig cfg, System.Random rng, DungeonGrid grid)
    {
        if (grid.RoomCenters.Count == 0) return;

        Vector2Int start = grid.RoomCenters[0];
        for (int i = 1; i < grid.RoomCenters.Count; i++)
            CarveCorridor(cfg, rng, grid, start, grid.RoomCenters[i]);

        int extra = Mathf.Max(1, grid.RoomCenters.Count / 3);
        for (int e = 0; e < extra; e++)
        {
            int a = rng.Next(1, grid.RoomCenters.Count);
            int b = rng.Next(1, grid.RoomCenters.Count);
            if (a == b) continue;
            CarveCorridor(cfg, rng, grid, grid.RoomCenters[a], grid.RoomCenters[b]);
        }
    }

    private void CarveCorridor(DungeonConfig cfg, System.Random rng, DungeonGrid grid, Vector2Int from, Vector2Int to)
    {
        bool horizontalFirst = rng.NextDouble() < 0.5;
        if (horizontalFirst) { CarveLineX(cfg, grid, from.x, to.x, from.y); CarveLineY(cfg, grid, from.y, to.y, to.x); }
        else                 { CarveLineY(cfg, grid, from.y, to.y, from.x); CarveLineX(cfg, grid, from.x, to.x, to.y); }
    }
    private void CarveLineX(DungeonConfig cfg, DungeonGrid grid, int x0, int x1, int y)
    {
        if (x0 > x1) (x0, x1) = (x1, x0);
        for (int x = x0; x <= x1; x++) grid.CarveCell(x, y, cfg.corridorWidth);
    }
    private void CarveLineY(DungeonConfig cfg, DungeonGrid grid, int y0, int y1, int x)
    {
        if (y0 > y1) (y0, y1) = (y1, y0);
        for (int y = y0; y <= y1; y++) grid.CarveCell(x, y, cfg.corridorWidth);
    }
}
