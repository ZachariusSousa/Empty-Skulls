using UnityEngine;

public class FoliageSpawner : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] foliageSprites;

    [Header("Spawn Area (world units)")]
    public Rect area = new Rect(-50, -50, 100, 100);

    [Header("Settings")]
    [Range(0f, 1f)] public float density = 0.3f; // higher = more foliage
    public int maxObjects = 1000;                // total random points to test

    void Start()
    {
        SpawnFoliage();
    }

    void SpawnFoliage()
    {
        if (foliageSprites == null || foliageSprites.Length == 0)
        {
            Debug.LogWarning("No foliage sprites assigned to FoliageSpawner.");
            return;
        }

        Transform container = new GameObject("FoliageContainer").transform;
        container.SetParent(transform);

        int count = Mathf.RoundToInt(maxObjects * density);

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(area.xMin, area.xMax),
                Random.Range(area.yMin, area.yMax)
            );

            var sprite = foliageSprites[Random.Range(0, foliageSprites.Length)];

            GameObject go = new GameObject("Foliage");
            go.transform.SetParent(container, false);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);
        }

        Debug.Log($"Spawned {count} foliage sprites.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawCube(area.center, area.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
