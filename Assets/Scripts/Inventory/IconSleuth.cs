using UnityEngine;
using UnityEngine.UI;

/// Attach this to the Icon object (the child that holds the Image).
/// It will log *who/when* the Image gets removed, the object is disabled,
/// or the parent changes. Works in Play Mode and in the Editor.
[ExecuteAlways]
[DisallowMultipleComponent]
public class IconSleuth : MonoBehaviour
{
    Image img;
    Image lastImg;                  // to detect recreation
    Transform lastParent;
    bool lastGOEnabled;
    bool lastImgEnabled;

    void OnEnable()
    {
        // Ensure logs include full stack traces
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);

        CacheRefs();
        lastGOEnabled = gameObject.activeInHierarchy;
        lastImgEnabled = img ? img.enabled : false;

        Debug.Log(Tag("OnEnable"));
    }

    void Awake()
    {
        CacheRefs();
        Debug.Log(Tag("Awake"));
    }

    void Start()
    {
        Debug.Log(Tag("Start"));
    }

    void OnDisable()
    {
        Debug.Log(Tag("OnDisable"));
    }

    void OnDestroy()
    {
        Debug.Log(Tag("OnDestroy (Icon GameObject)"));
    }

    void OnTransformParentChanged()
    {
        Debug.Log(Tag($"Parent changed → {transform.parent?.name ?? "null"}"));
        lastParent = transform.parent;
    }

    void Update()
    {
        // Parent change (runtime + editor)
        if (transform.parent != lastParent)
        {
            Debug.Log(Tag($"Parent changed → {transform.parent?.name ?? "null"}"));
            lastParent = transform.parent;
        }

        // GameObject active state change
        if (gameObject.activeInHierarchy != lastGOEnabled)
        {
            Debug.Log(Tag($"GameObject activeInHierarchy: {lastGOEnabled} → {gameObject.activeInHierarchy}"));
            lastGOEnabled = gameObject.activeInHierarchy;
        }

        // Image existence / recreation
        var current = GetComponent<Image>();
        if (img != current)
        {
            if (img == null && current != null)
                Debug.Log(Tag("Image component APPEARED (added/recreated)"));
            else if (img != null && current == null)
                Debug.LogError(Tag("Image component REMOVED"));

            lastImg = img;
            img = current;
        }

        // Image enabled flag change
        if (img)
        {
            if (img.enabled != lastImgEnabled)
            {
                Debug.Log(Tag($"Image.enabled: {lastImgEnabled} → {img.enabled}"));
                lastImgEnabled = img.enabled;
            }
        }
    }

    void CacheRefs()
    {
        img = GetComponent<Image>();
        lastImg = img;
        lastParent = transform.parent;
    }

    string Tag(string msg)
    {
        return $"[IconSleuth] '{name}' under '{transform.parent?.name ?? "null"}' | {msg}";
    }
}
