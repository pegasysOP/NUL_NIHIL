using Unity.VisualScripting;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private GroundCheck groundCheck;
    [SerializeField] private PlayerInputHandler input;

    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float airControlMult = 0.8f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Gravity")]
    public float gravity = -30f;
    public float maxFallSpeed = -18f;
    public float jumpCutMult = 2f;

    [Header("Settings")]
    public float snapDistance = 0.0625f;
    public float maxSlopeAngle = 45f;

    private Rigidbody2D rb;

    private Vector2 velocity;
    private Vector2 moveInput;
    private bool jumpHeld;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isGrounded = false;
    private bool wasGrounded = false;
    private bool justLanded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = input.MoveInput;
        jumpHeld = input.JumpHeld;

        if (input.JumpPressed)
            jumpBufferTimer = jumpBufferTime;

        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        wasGrounded = groundCheck.IsGrounded;
        groundCheck.Check();
        isGrounded = groundCheck.IsGrounded;
        justLanded = isGrounded && !wasGrounded;

        if (groundCheck.IsGrounded)
            coyoteTimer = coyoteTime;

        HandleHorizontal();
        TryJump();
        HandleVertical();

        Vector2 targetPos = rb.position + velocity * Time.fixedDeltaTime;

        RaycastHit2D snapHit = Physics2D.Raycast(targetPos + Vector2.up * 0.01f, Vector2.down, snapDistance + 0.01f, groundCheck.GroundLayer);

        // Snap to ground if we're grounded and not jumping away
        if (isGrounded && velocity.y <= 0f && snapHit)
        {
            targetPos.y = snapHit.point.y;
            velocity.y = 0f;
        }

        rb.MovePosition(targetPos);
    }

    private void TryJump()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            return;

        velocity.y = jumpForce;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void HandleHorizontal()
    {
        float targetSpeed = moveInput.x * maxSpeed;
        float accel = Mathf.Abs(targetSpeed) > Mathf.Epsilon ? acceleration : deceleration;

        // Aerial
        if (!isGrounded)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * airControlMult * Time.fixedDeltaTime);
            return;
        }

        // Slope threshold check
        float slopeAngle = Vector2.Angle(groundCheck.GroundNormal, Vector2.up);
        if (slopeAngle > maxSlopeAngle)
        {
            // too steep
            if (Mathf.Abs(moveInput.x) < Mathf.Epsilon)
            {
                velocity = Vector2.zero;
            }
            return;
        }

        // Grounded
        Vector2 tangent = Vector2.Perpendicular(groundCheck.GroundNormal).normalized;
        if (Vector2.Dot(tangent, Vector2.right) < 0f)
            tangent = -tangent;

        if (justLanded)
            velocity.y = 0f;
        float currentSpeed = Vector2.Dot(velocity, tangent);

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

        Vector2 slopeVelocity = tangent * newSpeed;
        velocity.y = slopeVelocity.y;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
    }

    private void HandleVertical()
    {
        // don't apply gravity when grounded and moving down/flat
        if (isGrounded && velocity.y <= 0f)
        {
            return;
        }

        // Apply gravity
        float gravityStep = gravity;

        if (velocity.y > 0f && !jumpHeld)
            gravityStep *= jumpCutMult;

        velocity.y += gravityStep * Time.fixedDeltaTime;

        if (velocity.y < maxFallSpeed)
            velocity.y = maxFallSpeed;
    }
}