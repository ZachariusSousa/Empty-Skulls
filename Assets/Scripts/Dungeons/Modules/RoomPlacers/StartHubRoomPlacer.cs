using UnityEngine;

[CreateAssetMenu(menuName = "Dungeons/Modules/Room Placer - Start Hub", fileName = "StartHubRoomPlacer")]
public class StartHubRoomPlacer : ScriptableObject, IRoomPlacer
{
    public void PlaceRooms(DungeonConfig cfg, System.Random rng, DungeonGrid grid)
    {
        // Start room centered
        Vector2Int half = new(cfg.width / 2, cfg.height / 2);
        int sw = Mathf.Clamp(cfg.startRoomSize.x, 4, Mathf.Max(4, cfg.width - 4));
        int sh = Mathf.Clamp(cfg.startRoomSize.y, 4, Mathf.Max(4, cfg.height - 4));

        var start = new RectInt(
            Mathf.Clamp(half.x - sw / 2, 1, cfg.width - sw - 1),
            Mathf.Clamp(half.y - sh / 2, 1, cfg.height - sh - 1),
            sw, sh
        );
        grid.CarveRoom(start);
        grid.Rooms.Add(start);
        grid.StartCenterCell = new Vector2Int(start.xMin + start.width / 2, start.yMin + start.height / 2);
        grid.RoomCenters.Add(grid.StartCenterCell);

        // Other rooms (non-overlapping with 1-tile padding)
        int attempts = 0;
        while (grid.Rooms.Count < cfg.roomCount && attempts < cfg.placementAttempts)
        {
            attempts++;
            int w = rng.Next(cfg.roomSizeMinMax.x, cfg.roomSizeMinMax.y + 1);
            int h = rng.Next(cfg.roomSizeMinMax.x, cfg.roomSizeMinMax.y + 1);
            int x = rng.Next(1, Mathf.Max(2, cfg.width - w - 1));
            int y = rng.Next(1, Mathf.Max(2, cfg.height - h - 1));
            var room = new RectInt(x, y, w, h);

            var padded = new RectInt(room.xMin - 1, room.yMin - 1, room.width + 2, room.height + 2);
            bool overlaps = false;
            foreach (var r in grid.Rooms)
            {
                var pr = new RectInt(r.xMin - 1, r.yMin - 1, r.width + 2, r.height + 2);
                if (padded.Overlaps(pr)) { overlaps = true; break; }
            }
            if (overlaps) continue;

            grid.CarveRoom(room);
            grid.Rooms.Add(room);
            grid.RoomCenters.Add(new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2));
        }
    }
}
