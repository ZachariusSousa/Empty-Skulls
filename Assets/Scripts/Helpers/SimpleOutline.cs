using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleOutline : MonoBehaviour
{
    public Color color = Color.black;
    public int pixels = 1; // 1 = one pixel thick

    SpriteRenderer main;
    SpriteRenderer[] outs;

    void Awake()
    {
        main = GetComponent<SpriteRenderer>();
        float u = pixels / (main.sprite ? main.sprite.pixelsPerUnit : 16f);

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        outs = new SpriteRenderer[dirs.Length];

        for (int i = 0; i < dirs.Length; i++)
        {
            var go = new GameObject("_o" + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)(dirs[i] * u);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = main.sprite;
            sr.color = color;
            sr.sortingLayerID = main.sortingLayerID;
            sr.sortingOrder = main.sortingOrder - 1; // behind
            outs[i] = sr;
        }
    }

    void LateUpdate()
    {
        foreach (var sr in outs)
        {
            sr.sprite = main.sprite;
            sr.flipX  = main.flipX;
            sr.flipY  = main.flipY;
        }
    }
}
