using UnityEngine;
using UnityEngine.Tilemaps;

public interface IRoomPlacer
{
    void PlaceRooms(DungeonConfig cfg, System.Random rng, DungeonGrid grid);
}

public interface ICorridorConnector
{
    void Connect(DungeonConfig cfg, System.Random rng, DungeonGrid grid);
}

public interface ITilePainter2D
{
    void Paint(DungeonConfig cfg, DungeonTheme2D theme, DungeonGrid grid,
               Tilemap ground, Tilemap walls, Vector3Int paintOffset);
}

public interface IDungeonPostProcessor
{
    void PostProcess(DungeonConfig cfg, System.Random rng, DungeonGrid grid,
                     Tilemap ground, Tilemap walls, Vector3Int paintOffset, Transform parent);
}
