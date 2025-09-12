using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeons/PostProcess/Breakables in Corridors", fileName = "Breakables")]
public class Breakables : ScriptableObject, IDungeonPostProcessor
{
    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float spawnChance = 0.08f;
    public GameObject breakablePrefab;

    public void PostProcess(DungeonConfig cfg, System.Random rng, DungeonGrid grid,
                            Tilemap ground, Tilemap walls, Vector3Int paintOffset, Transform parent)
    {
        if (!breakablePrefab) return;

        bool IsInsideAnyRoom(int x, int y)
        {
            foreach (var r in grid.Rooms)
                if (r.Contains(new Vector2Int(x, y))) return true;
            return false;
        }

        for (int x = 0; x < cfg.width; x++)
        for (int y = 0; y < cfg.height; y++)
        {
            if (!grid.Walkable[x, y]) continue;
            if (IsInsideAnyRoom(x, y)) continue; // corridors only
            if (rng.NextDouble() < spawnChance)
            {
                var world = ground.GetCellCenterWorld(new Vector3Int(x, y, 0) + paintOffset);
                Object.Instantiate(breakablePrefab, world, Quaternion.identity, parent);
            }
        }
    }
}
