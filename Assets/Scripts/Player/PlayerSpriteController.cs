using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteController : MonoBehaviour
{
    private enum Facing { Right, Left, Up, Down }

    [Header("Two frames per direction (0 = base, 1 = walk)")]
    public Sprite[] right = new Sprite[2];
    public Sprite[] left  = new Sprite[2];
    public Sprite[] up    = new Sprite[2];
    public Sprite[] down  = new Sprite[2];

    [Header("Options")]
    public bool mirrorLeftIfEmpty = true;
    public float moveThreshold = 0.05f;
    [Tooltip("Seconds between frame swaps while moving")]
    public float secondsPerSwap = 0.12f;

    [Header("Motion Source")]
    public Rigidbody2D rbOverride;          // leave empty to auto-find
    public bool useExternalInput = false;   // if true, use externalMoveInput instead of rb velocity
    public Vector2 externalMoveInput;

    SpriteRenderer _sr;
    Rigidbody2D _rb;
    Facing _facing = Facing.Down;
    float _timer;
    int _frame; // 0 or 1

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = rbOverride ? rbOverride : GetComponent<Rigidbody2D>();
        if (!_rb) _rb = GetComponentInParent<Rigidbody2D>();
    }

    void Update()
    {
        // --- velocity source ---
        Vector2 v = useExternalInput ? externalMoveInput
                                     : (_rb ? _rb.linearVelocity : Vector2.zero);

        bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;

        // --- facing from velocity (stick to last if idle) ---
        if (moving)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                _facing = v.x >= 0 ? Facing.Right : Facing.Left;
            else
                _facing = v.y >= 0 ? Facing.Up : Facing.Down;
        }

        // --- frame toggle when moving ---
        if (moving)
        {
            _timer += Time.deltaTime;
            if (_timer >= Mathf.Max(0.01f, secondsPerSwap))
            {
                _timer = 0f;
                _frame = 1 - _frame; // 0 <-> 1
            }
        }
        else
        {
            _timer = 0f;
            _frame = 0; // idle frame
        }

        // --- choose sprite ---
        bool flipX = false;
        Sprite s = GetSpriteFor(_facing, _frame, ref flipX);

        if (s) { _sr.sprite = s; _sr.flipX = flipX; }
    }

    Sprite GetSpriteFor(Facing f, int frame, ref bool flipX)
    {
        frame = Mathf.Clamp(frame, 0, 1);

        switch (f)
        {
            case Facing.Right:
                return (right != null && right.Length > frame) ? right[frame] : null;

            case Facing.Left:
                if (left != null && left.Length > frame && left[frame] != null)
                    return left[frame];

                if (mirrorLeftIfEmpty && right != null && right.Length > frame)
                {
                    flipX = true;
                    return right[frame];
                }
                return null;

            case Facing.Up:
                return (up != null && up.Length > frame) ? up[frame] : null;

            default: // Down
                return (down != null && down.Length > frame) ? down[frame] : null;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        secondsPerSwap = Mathf.Max(0.01f, secondsPerSwap);
        moveThreshold = Mathf.Max(0f, moveThreshold);
    }
#endif
}
