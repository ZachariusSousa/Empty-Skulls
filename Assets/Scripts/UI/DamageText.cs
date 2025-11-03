using UnityEngine;
using TMPro; // gives us TMP_Text base class

[DisallowMultipleComponent]
public class DamageText : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text tmp; // works for both TextMeshPro and TextMeshProUGUI

    [Header("Motion")]
    public Vector3 startJitter = new Vector3(0.15f, 0.15f, 0f);
    public Vector3 riseVelocity = new Vector3(0f, 1.0f, 0f);
    public float lifetime = 0.6f;
    public float gravity = -1.5f;

    [Header("Scale")]
    public float startScale = 0.9f;
    public float popScale = 1.15f;
    public float popTime = 0.08f;

    float _t;
    Vector3 _vel;
    Color _color = Color.white;
    float _popT;

    void Reset() { AutoWireTMP(); }
    void OnValidate() { if (!tmp) AutoWireTMP(); }

    void Awake()
    {
        if (!tmp) AutoWireTMP();
        if (!tmp)
        {
            Debug.LogError("[DamageText] No TMP component found on this object or its children. Add TextMeshPro or TextMeshProUGUI.", this);
            enabled = false;
        }
    }

    void AutoWireTMP()
    {
        tmp = GetComponent<TMP_Text>();
        if (!tmp) tmp = GetComponentInChildren<TMP_Text>(true);
    }

    public void Play(int amount, Vector3 worldPos, Color color, bool crit = false)
    {
        if (!tmp) { AutoWireTMP(); if (!tmp) { enabled = false; return; } }

        transform.position = worldPos
            + new Vector3(Random.Range(-startJitter.x, startJitter.x),
                          Random.Range(-startJitter.y, startJitter.y), 0f);

        tmp.text = amount.ToString();
        _color = color;
        tmp.color = _color;

        _t = 0f;
        _popT = 0f;
        transform.localScale = Vector3.one * (crit ? popScale : startScale);
        _vel = riseVelocity + new Vector3(Random.Range(-0.15f, 0.15f), 0f, 0f);
        gameObject.SetActive(true);
        enabled = true; 
    }

    void Update()
    {
        if (!tmp) return; 

        float dt = Time.deltaTime;
        _t += dt;

        // motion
        _vel.y += gravity * dt * 0.5f;
        transform.position += _vel * dt;

        // pop → settle scale
        if (_popT < popTime) _popT += dt;
        float k = Mathf.Clamp01(_popT / popTime);
        float scale = Mathf.Lerp(transform.localScale.x, 1f, k);
        transform.localScale = Vector3.one * scale;

        // fade during last 40% of lifetime
        float a = Mathf.InverseLerp(lifetime, lifetime * 0.6f, _t);
        var c = _color; c.a = 1f - Mathf.Clamp01(a);
        tmp.color = c;

        if (_t >= lifetime)
            DamageTextPool.Release(this);
    }
}
