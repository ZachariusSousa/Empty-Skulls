using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteController : MonoBehaviour
{
    public enum Pose { Base = 0, Walk = 1, Shoot = 2, WalkShoot = 3 }
    private enum Facing { Right, Left, Up, Down }

    [Header("Directional Frames (index: 0=Base, 1=Walk, 2=Shoot, 3=WalkShoot)")]
    public Sprite[] rightFrames = new Sprite[4];
    public Sprite[] leftFrames  = new Sprite[4];
    public Sprite[] upFrames    = new Sprite[4];
    public Sprite[] downFrames  = new Sprite[4];

    [Header("Options")]
    public bool mirrorLeftIfEmpty = true;
    public float moveThreshold = 0.05f;
    [Tooltip("Seconds between base↔walk while moving (simple 2-state step)")]
    public float secondsPerSwap = 0.12f;

    [Header("Input Source")]
    public bool useExternalInput = false;
    public Vector2 externalMoveInput;
    public Rigidbody2D rbOverride;

    [Header("Shooting")]
    [Tooltip("How long to show the shoot variants after TriggerShoot()")]
    public float shootHoldSeconds = 0.15f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Vector3 lastPos;
    private float swapTimer = 0f;
    private bool showWalk = false;
    private float shootTimer = 0f;
    private Facing lastFacing = Facing.Down;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = rbOverride ? rbOverride : GetComponent<Rigidbody2D>();
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
        lastPos = transform.position;
    }

    void Update()
    {
        // source velocity
        Vector2 v = useExternalInput ? externalMoveInput :
                    rb ? rb.linearVelocity :
                    (Vector2)((transform.position - lastPos) / Mathf.Max(Time.deltaTime, 0.0001f));
        lastPos = transform.position;

        bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;

        // facing
        if (moving)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                lastFacing = v.x >= 0 ? Facing.Right : Facing.Left;
            else
                lastFacing = v.y >= 0 ? Facing.Up : Facing.Down;
        }

        // walk toggle
        if (moving)
        {
            swapTimer += Time.deltaTime;
            if (swapTimer >= Mathf.Max(0.01f, secondsPerSwap))
            {
                swapTimer = 0f;
                showWalk = !showWalk;
            }
        }
        else
        {
            swapTimer = 0f;
            showWalk = false;
        }

        // shooting window countdown
        if (shootTimer > 0f) shootTimer -= Time.deltaTime;

        // choose pose
        Pose pose;
        bool shooting = shootTimer > 0f;

        if (moving)
            pose = shooting ? Pose.WalkShoot : Pose.Walk;
        else
            pose = shooting ? Pose.Shoot : Pose.Base;

        // If you want the walk variant to only appear on every-other toggle:
        // if (pose == Pose.Walk && !showWalk) pose = Pose.Base;
        // if (pose == Pose.WalkShoot && !showWalk) pose = Pose.Shoot;

        // pick sprite
        bool flipX;
        var frames = FramesFor(lastFacing, out flipX);

        int idx = (int)pose;
        Sprite s = (frames != null && frames.Length > idx) ? frames[idx] : null;

        if (s != null)
        {
            sr.sprite = s;
            sr.flipX = flipX;
        }
    }

    private Sprite[] FramesFor(Facing f, out bool flipX)
    {
        flipX = false;
        switch (f)
        {
            case Facing.Right: return rightFrames;
            case Facing.Left:
                if (leftFrames != null && leftFrames.Length >= 4 && leftFrames[0] != null)
                    return leftFrames;
                if (mirrorLeftIfEmpty && rightFrames != null && rightFrames.Length >= 4)
                {
                    flipX = true;
                    return rightFrames;
                }
                return leftFrames;
            case Facing.Up:    return upFrames;
            default:           return downFrames;
        }
    }

    /// <summary>
    /// Call this from your shooting code when a shot is fired.
    /// Optionally pass a custom duration.
    /// </summary>
    public void TriggerShoot(float duration = -1f)
    {
        shootTimer = (duration > 0f) ? duration : shootHoldSeconds;
        // Optional: snap to Shoot immediately (feels snappier)
        // swapTimer = 0f; showWalk = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        secondsPerSwap = Mathf.Max(0.01f, secondsPerSwap);
        moveThreshold = Mathf.Max(0f, moveThreshold);
        shootHoldSeconds = Mathf.Max(0.01f, shootHoldSeconds);
    }
#endif
}
