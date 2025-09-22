using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("Idle")]
    public Sprite idleRight, idleLeft, idleUp, idleDown;

    [Header("Walk (single frame each)")]
    public Sprite walkRight, walkLeft, walkUp, walkDown;

    [Header("Options")]
    public bool mirrorLeftIfEmpty = true;
    public float moveThreshold = 0.05f;

    [Tooltip("Swap between idle/walk every X seconds while moving")]
    public float secondsPerSwap = 0.12f;

    [Header("Input Source")]
    public bool useExternalInput = false;   // set true if you feed input
    public Vector2 externalMoveInput;       // assign from movement script
    public Rigidbody2D rbOverride;          // drag parent RB if you want

    SpriteRenderer sr;
    Rigidbody2D rb;
    Vector3 lastPos;
    float swapTimer = 0f;
    bool showWalkFrame = false;

    enum Facing { Right, Left, Up, Down }
    Facing lastFacing = Facing.Down;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = rbOverride ? rbOverride : GetComponent<Rigidbody2D>();
        if (!rb) rb = GetComponentInParent<Rigidbody2D>(); // parent RB support
        lastPos = transform.position;
    }

    void Update()
    {
        // velocity source
        Vector2 v = useExternalInput ? externalMoveInput :
                    rb ? rb.linearVelocity :
                    (Vector2)((transform.position - lastPos) / Mathf.Max(Time.deltaTime, 0.0001f));
        lastPos = transform.position;

        bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;

        // facing from movement
        if (moving)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                lastFacing = v.x >= 0 ? Facing.Right : Facing.Left;
            else
                lastFacing = v.y >= 0 ? Facing.Up : Facing.Down;

            // time-based toggle
            swapTimer += Time.deltaTime;
            if (swapTimer >= Mathf.Max(0.01f, secondsPerSwap))
            {
                swapTimer = 0f;
                showWalkFrame = !showWalkFrame;
            }
        }
        else
        {
            swapTimer = 0f;
            showWalkFrame = false; // show idle when stopped
        }

        // pick sprite
        Sprite s = null; bool flipX = false;
        switch (lastFacing)
        {
            case Facing.Right:
                s = showWalkFrame && moving ? (walkRight ? walkRight : idleRight) : idleRight;
                break;

            case Facing.Left:
                if (showWalkFrame && moving)
                {
                    if (walkLeft) s = walkLeft;
                    else if (mirrorLeftIfEmpty && walkRight) { s = walkRight; flipX = true; }
                    else s = idleLeft ? idleLeft : idleRight;
                }
                else
                {
                    if (idleLeft) s = idleLeft;
                    else if (mirrorLeftIfEmpty && idleRight) { s = idleRight; flipX = true; }
                    else s = idleRight;
                }
                break;

            case Facing.Up:
                s = showWalkFrame && moving ? (walkUp ? walkUp : idleUp) : (idleUp ? idleUp : idleRight);
                break;

            default: // Down
                s = showWalkFrame && moving ? (walkDown ? walkDown : idleDown) : (idleDown ? idleDown : idleRight);
                break;
        }

        if (s) { sr.sprite = s; sr.flipX = flipX; }
    }
}
