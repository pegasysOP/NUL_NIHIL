using UnityEngine;


public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Material flashMaterial;

    [Header("Damage")]
    public float flashDuration = 0.083f;
    public float blinkHz = 10f;

    private Material baseMaterial;

    private enum State { Idle, Run, Jump, Fall }

    private static readonly int[] stateHashes =
    {
        Animator.StringToHash("Idle"),
        Animator.StringToHash("Run"),
        Animator.StringToHash("Jump"),
        Animator.StringToHash("Fall"),
    };

    private State state = State.Idle;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponentInParent<PlayerMovement>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponentInParent<PlayerHealth>();

        baseMaterial = spriteRenderer.sharedMaterial;

        if (movement == null)
            Debug.LogError("PlayerVisuals: no PlayerMovement on this object or its parents.", this);

        if (health == null)
            Debug.LogError("PlayerVisuals: no PlayerHealth on this object or its parents.", this);

        if (flashMaterial == null)
            Debug.LogError("PlayerVisuals: no flash material assigned.", this);
    }

    private void LateUpdate()
    {
        // hold the current pose through room transitions instead of snapping to idle
        animator.speed = movement.IsFrozen ? 0f : 1f;
        if (movement.IsFrozen)
        {
            // never carry a half blink or the flash material through a transition
            spriteRenderer.enabled = true;
            spriteRenderer.sharedMaterial = baseMaterial;
            return;
        }

        UpdateDamageVisual();

        // face the input direction, keep the last facing when idle
        if (movement.MoveDirection != 0f)
            spriteRenderer.flipX = movement.MoveDirection < 0f;

        State next = PickState();
        if (next == state)
            return;

        state = next;
        animator.Play(stateHashes[(int)state]);
    }

    // white flash for the first animation frame's worth of the hit then blink
    // for the rest of the iframe window
    private void UpdateDamageVisual()
    {
        if (!health.IsInvulnerable)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sharedMaterial = baseMaterial;
            return;
        }

        float elapsed = health.InvulnerabilityElapsed;

        if (elapsed < flashDuration)
        {
            spriteRenderer.enabled = true;
            if (flashMaterial != null)
                spriteRenderer.sharedMaterial = flashMaterial;
            return;
        }

        spriteRenderer.sharedMaterial = baseMaterial;
        spriteRenderer.enabled = Mathf.FloorToInt((elapsed - flashDuration) * blinkHz) % 2 == 0;
    }

    private State PickState()
    {
        if (!movement.IsGrounded)
            return movement.Velocity.y > 0f ? State.Jump : State.Fall;

        return movement.MoveDirection != 0f ? State.Run : State.Idle;
    }
}
