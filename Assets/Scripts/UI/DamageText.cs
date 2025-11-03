using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DamageText : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text tmp;

    [Header("Motion")]
    public Vector3 startJitter = new Vector3(0.15f, 0.15f, 0f);
    public Vector3 riseVelocity = new Vector3(0f, 1.0f, 0f);
    public float lifetime = 0.6f;
    public float gravity = -1.5f;

    [Header("Scale")]
    public float startScale = 0.9f;
    public float popScale = 1.15f;
    public float popTime = 0.08f;

    [Header("Spawn Offset")]
    public Vector3 spawnOffset = new Vector3(0f, 0.8f, 0f);

    float _t;
    Vector3 _vel;
    Color _baseColor = Color.red;
    float _popT;

    void Reset() => AutoWireTMP();
    void OnValidate() { if (!tmp) AutoWireTMP(); }

    void Awake()
    {
        if (!tmp) AutoWireTMP();
        if (!tmp)
        {
            Debug.LogError("[DamageText] No TMP component found.", this);
            enabled = false;
            return;
        }
    }

    void AutoWireTMP()
    {
        tmp = GetComponent<TMP_Text>();
        if (!tmp) tmp = GetComponentInChildren<TMP_Text>(true);
    }

    public void Play(int amount, Vector3 worldPos, Color _ = default, bool crit = false)
    {
        if (!tmp) { enabled = false; return; }

        // Apply the spawn offset + jitter
        transform.position = worldPos + spawnOffset
            + new Vector3(Random.Range(-startJitter.x, startJitter.x),
                          Random.Range(-startJitter.y, startJitter.y), 0f);

        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * (crit ? popScale : startScale);

        tmp.text = amount.ToString();
        tmp.color = _baseColor;

        _vel = riseVelocity + new Vector3(Random.Range(-0.15f, 0.15f), 0f, 0f);
        _t = 0f;
        _popT = 0f;
        enabled = true;
    }

    void Update()
    {
        if (!tmp) return;

        float dt = Time.deltaTime;
        _t += dt;

        _vel.y += gravity * dt * 0.5f;
        transform.position += _vel * dt;

        if (_popT < popTime) _popT += dt;
        float k = Mathf.Clamp01(_popT / popTime);
        float scale = Mathf.Lerp(transform.localScale.x, 1f, k);
        transform.localScale = Vector3.one * scale;

        float life01 = Mathf.Clamp01(_t / lifetime);
        float fade01 = Mathf.InverseLerp(0.6f, 1f, life01);
        var c = _baseColor; c.a = 1f - fade01;
        tmp.color = c;

        if (_t >= lifetime)
            DamageTextPool.Release(this);
    }
}
