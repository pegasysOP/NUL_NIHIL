using UnityEngine;


public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

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

        if (movement == null)
            Debug.LogError("PlayerVisuals: no PlayerMovement on this object or its parents.", this);
    }

    private void LateUpdate()
    {
        // hold the current pose through room transitions instead of snapping to idle
        animator.speed = movement.IsFrozen ? 0f : 1f;
        if (movement.IsFrozen)
            return;

        // face the input direction, keep the last facing when idle
        if (movement.MoveDirection != 0f)
            spriteRenderer.flipX = movement.MoveDirection < 0f;

        State next = PickState();
        if (next == state)
            return;

        state = next;
        animator.Play(stateHashes[(int)state]);
    }

    private State PickState()
    {
        if (!movement.IsGrounded)
            return movement.Velocity.y > 0f ? State.Jump : State.Fall;

        return movement.MoveDirection != 0f ? State.Run : State.Idle;
    }
}
