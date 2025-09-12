using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeons/Dungeon Theme 2D", fileName = "DungeonTheme2D")]
public class DungeonTheme2D : ScriptableObject
{
    [Header("Tiles")]
    public TileBase floorTile;
    public TileBase wallTile;
}
