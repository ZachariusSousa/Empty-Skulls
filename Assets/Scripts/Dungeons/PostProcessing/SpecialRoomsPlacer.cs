using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeons/PostProcess/Special Rooms (Boss & Trophy)", fileName = "SpecialRoomsPlacer")]
public class SpecialRoomsPlacer : ScriptableObject, IDungeonPostProcessor
{
    public GameObject bossRoomMarker;
    public GameObject trophyRoomMarker;

    public void PostProcess(DungeonConfig cfg, System.Random rng, DungeonGrid grid,
                            Tilemap ground, Tilemap walls, Vector3Int paintOffset, Transform parent)
    {
        if (grid.Rooms.Count <= 1) return;

        var start = grid.StartCenterCell;
        int bossIdx = -1; float far = -1f;
        int trophyIdx = -1; float second = -1f;

        for (int i = 1; i < grid.RoomCenters.Count; i++)
        {
            float d = Vector2Int.Distance(start, grid.RoomCenters[i]);
            if (d > far) { second = far; trophyIdx = bossIdx; far = d; bossIdx = i; }
            else if (d > second) { second = d; trophyIdx = i; }
        }

        if (bossIdx != -1 && bossRoomMarker)
            InstantiateAtCell(ground, paintOffset, parent, grid.RoomCenters[bossIdx], bossRoomMarker);
        if (trophyIdx != -1 && trophyRoomMarker)
            InstantiateAtCell(ground, paintOffset, parent, grid.RoomCenters[trophyIdx], trophyRoomMarker);
    }

    private void InstantiateAtCell(Tilemap ground, Vector3Int paintOffset, Transform parent, Vector2Int cell, GameObject prefab)
    {
        var p = ground.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0) + paintOffset);
        Object.Instantiate(prefab, p, Quaternion.identity, parent);
    }
}
