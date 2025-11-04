using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LowHealthTint : MonoBehaviour
{
    public PlayerStats player;
    public Image image;
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    [Range(0f,1f)] public float threshold = 0.35f;
    [Range(0f,1f)] public float maxAlpha = 0.6f;
    public float pulseMinHz = 0.8f;
    public float pulseMaxHz = 2.2f;
    public float smooth = 10f;

    void Awake()
    {
        if (autoFindPlayerByTag && player == null)
        {
            var t = GameObject.FindGameObjectWithTag(playerTag);
            if (t) player = t.GetComponent<PlayerStats>();
        }
        if (image == null) image = GetComponent<Image>();
        if (image != null)
        {
            var c = image.color; c.a = 0f; image.color = c;
            image.raycastTarget = false;
        }
    }

    void OnEnable()
    {
        if (player != null) player.onStatChanged.AddListener(OnStatChanged);
    }

    void OnDisable()
    {
        if (player != null) player.onStatChanged.RemoveListener(OnStatChanged);
    }

    void Update()
    {
        if (player == null || image == null) return;

        float frac = Mathf.Clamp01((float)player.HP / player.EffMaxHP);
        float a = 0f;

        if (frac < threshold)
        {
            float t = 1f - frac / threshold;
            float hz = Mathf.Lerp(pulseMinHz, pulseMaxHz, t);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * hz);
            a = Mathf.Lerp(0f, maxAlpha, t) * Mathf.Lerp(0.6f, 1f, pulse);
        }

        var c = image.color;
        c.a = Mathf.Lerp(c.a, a, Time.unscaledDeltaTime * smooth);
        image.color = c;
    }

    void OnStatChanged(string s, int _)
    {
        if (s == "hp" || s == "maxHP" || s == "maxHP_eff") { }
    }
}
