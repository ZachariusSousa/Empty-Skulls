using UnityEngine;

[CreateAssetMenu(menuName = "Dungeons/Dungeon Config", fileName = "DungeonConfig")]
public class DungeonConfig : ScriptableObject
{
    [Header("Grid Size (logical cells)")]
    [Min(8)] public int width = 80;
    [Min(8)] public int height = 50;

    [Header("Rooms")]
    [Min(1)] public int roomCount = 12;
    public Vector2Int roomSizeMinMax = new Vector2Int(6, 12);
    [Min(1)] public int placementAttempts = 200;
    [Min(1)] public int corridorWidth = 1;

    [Header("Start Room")]
    public Vector2Int startRoomSize = new Vector2Int(10, 8);

    [Header("Randomness")]
    public string seed = "my-seed-001";
    public bool useRandomSeed = false;
}
