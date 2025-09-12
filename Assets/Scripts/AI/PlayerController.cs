using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum ControlMode { Manual, AI }
    public ControlMode mode = ControlMode.Manual;

    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private Vector2 moveInput;

    // AI wandering
    private Vector2 aiTarget;
    private float aiChangeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();

        // Bind movement input
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // Toggle mode at runtime
            mode = (mode == ControlMode.Manual) ? ControlMode.AI : ControlMode.Manual;
            Debug.Log("Switched mode: " + mode);
        }

        if (mode == ControlMode.AI)
        {
            HandleAI();
        }
    }

    private void FixedUpdate()
    {
        Vector2 velocity = Vector2.zero;

        if (mode == ControlMode.Manual)
        {
            velocity = moveInput.normalized * moveSpeed;
        }
        else if (mode == ControlMode.AI)
        {
            velocity = (aiTarget - rb.position).normalized * moveSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void HandleAI()
    {
        aiChangeTimer -= Time.deltaTime;

        // Pick a new random target every few seconds
        if (aiChangeTimer <= 0f || Vector2.Distance(rb.position, aiTarget) < 0.5f)
        {
            aiTarget = rb.position + Random.insideUnitCircle * 5f; // random wander radius
            aiChangeTimer = Random.Range(2f, 4f);
        }
    }
}
