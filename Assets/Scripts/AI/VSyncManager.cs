using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class VSyncManager : MonoBehaviour
{
    private static VSyncManager _instance;

    [Tooltip("0=Off, 1=Every VBlank (~monitor Hz), 2=Every 2nd VBlank (~Hz/2)")]
    [Range(0,2)] public int vSyncCount = 1;

    [Tooltip("Used only when vSyncCount=0. -1=platform default")]
    public int targetFrameRate = -1;

    [Tooltip("Keep this object across scenes")]
    public bool dontDestroyOnLoad = true;

    [Header("Optional debug toggle")]
    public KeyCode toggleKey = KeyCode.None; // e.g., KeyCode.F10

    void Awake()
    {
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        Apply();
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            vSyncCount = (vSyncCount == 0) ? 1 : 0;
            Apply();
        }
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = (vSyncCount == 0) ? targetFrameRate : -1;
    }

    // Public helpers if you want to change it from other scripts:
    public static void SetVSync(int count) { if (_instance) { _instance.vSyncCount = Mathf.Clamp(count,0,2); _instance.Apply(); } }
    public static void SetTargetFps(int fps) { if (_instance) { _instance.targetFrameRate = fps; if (_instance.vSyncCount==0) Application.targetFrameRate=fps; } }
}
