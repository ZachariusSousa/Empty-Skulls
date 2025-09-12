using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator2D : MonoBehaviour
{
    [Header("Scene Refs (can be left empty if Autowire is ON)")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap;

    [Header("Data (ScriptableObjects)")]
    public DungeonConfig config;
    public DungeonTheme2D theme;

    [Header("Strategy Modules (ScriptableObjects)")]
    public ScriptableObject roomPlacerAsset;        // IRoomPlacer
    public ScriptableObject corridorConnectorAsset; // ICorridorConnector
    public ScriptableObject tilePainterAsset;       // ITilePainter2D

    [Header("Post-Processors (ScriptableObjects)")]
    public List<ScriptableObject> postProcessors;   // each IDungeonPostProcessor

    [Header("Autowire / Create Children")]
    [Tooltip("If true, will find or create Grid/Ground/Walls as children of this prefab.")]
    public bool autoWireTilemaps = true;

    // Runtime
    private System.Random _rng;
    private DungeonGrid _grid;
    private Vector3Int _paintOffset;

    private IRoomPlacer RoomPlacer => roomPlacerAsset as IRoomPlacer;
    private ICorridorConnector CorridorConnector => corridorConnectorAsset as ICorridorConnector;
    private ITilePainter2D TilePainter => tilePainterAsset as ITilePainter2D;

    private void Start() => Generate();

    // ---------- Public API (multiplayer / external control) ----------
    public void GenerateWithSeed(int seedOverride)
    {
        if (autoWireTilemaps) FindOrCreateTilemaps();
        if (!ValidateInspector()) return;

        _rng = new System.Random(seedOverride);
        InternalGenerate();
    }

    public void Generate()
    {
        if (autoWireTilemaps) FindOrCreateTilemaps();
        if (!ValidateInspector()) return;

        int s = (config.useRandomSeed)
            ? UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            : (config.seed?.GetHashCode() ?? 0);
        _rng = new System.Random(s);

        InternalGenerate();
    }

    private void InternalGenerate()
    {
        _grid = new DungeonGrid(config.width, config.height);
        _paintOffset = new Vector3Int(-config.width / 2, -config.height / 2, 0);

        RoomPlacer.PlaceRooms(config, _rng, _grid);
        CorridorConnector.Connect(config, _rng, _grid);
        TilePainter.Paint(config, theme, _grid, groundTilemap, wallTilemap, _paintOffset);

        // Ensure a local "Content" child to keep spawned things inside the prefab instance
        var content = transform.Find("Content");
        if (!content)
        {
            var c = new GameObject("Content");
            c.transform.SetParent(transform, false);
            content = c.transform;
        }
        else
        {
#if UNITY_EDITOR
            // Clear old children in editor when regenerating
            for (int i = content.childCount - 1; i >= 0; i--)
                DestroyImmediate(content.GetChild(i).gameObject);
#endif
        }

        if (postProcessors != null)
        {
            foreach (var so in postProcessors)
                if (so is IDungeonPostProcessor pp)
                    pp.PostProcess(config, _rng, _grid, groundTilemap, wallTilemap, _paintOffset, content);
        }
    }

    private bool ValidateInspector()
    {
        if (!config || !theme) { Debug.LogError("Assign DungeonConfig & DungeonTheme2D assets."); return false; }
        if (!theme.floorTile || !theme.wallTile) { Debug.LogError("Theme missing tiles."); return false; }
        if (!(roomPlacerAsset is IRoomPlacer)) { Debug.LogError("roomPlacerAsset must implement IRoomPlacer."); return false; }
        if (!(corridorConnectorAsset is ICorridorConnector)) { Debug.LogError("corridorConnectorAsset must implement ICorridorConnector."); return false; }
        if (!(tilePainterAsset is ITilePainter2D)) { Debug.LogError("tilePainterAsset must implement ITilePainter2D."); return false; }
        if (!groundTilemap || !wallTilemap) { Debug.LogError("Ground/Walls Tilemaps not assigned or found."); return false; }
        return true;
    }

    // ---------- Autowire helpers (prefab-safe) ----------
    [ContextMenu("Autowire Tilemaps")]
    private void AutowireTilemapsContext() => FindOrCreateTilemaps();

    private void Reset() { FindOrCreateTilemaps(); }
    private void OnValidate() { if (!Application.isPlaying) FindOrCreateTilemaps(); }

    private void FindOrCreateTilemaps()
    {
        if (!autoWireTilemaps) return;

        // Ensure Grid child
        var grid = GetComponentInChildren<Grid>(true);
        if (!grid)
        {
            var g = new GameObject("Grid", typeof(Grid));
            g.transform.SetParent(this.transform, false);
            grid = g.GetComponent<Grid>();
        }

        Tilemap GetOrCreate(string name)
        {
            var t = grid.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(tm => tm.gameObject.name == name);
            if (!t)
            {
                var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(grid.transform, false);
                t = go.GetComponent<Tilemap>();
            }
            return t;
        }

        if (!groundTilemap) groundTilemap = GetOrCreate("Ground");
        if (!wallTilemap)   wallTilemap   = GetOrCreate("Walls");
    }

    // ---------- Read-only access for spawners / netcode ----------
    public bool[,] GetWalkableGrid() => _grid?.Walkable;
    public Vector2Int GetStartCell() => _grid != null ? _grid.StartCenterCell : default;
    public Vector3Int GetPaintOffset() => _paintOffset;

    public Vector3 GetStartWorldPosition()
    {
        if (_grid == null || groundTilemap == null) return Vector3.zero;
        return groundTilemap.GetCellCenterWorld(new Vector3Int(_grid.StartCenterCell.x, _grid.StartCenterCell.y, 0) + _paintOffset);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_grid == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.18f);
        foreach (var r in _grid.Rooms)
        {
            var center = new Vector3(r.center.x, r.center.y, 0) + (Vector3)_paintOffset;
            var size = new Vector3(r.size.x, r.size.y, 0);
            Gizmos.DrawCube(center, size);
        }
        Gizmos.color = Color.yellow;
        foreach (var c in _grid.RoomCenters)
        {
            var p = new Vector3(c.x + 0.5f, c.y + 0.5f, 0) + (Vector3)_paintOffset;
            Gizmos.DrawSphere(p, 0.15f);
        }
    }
#endif
}
