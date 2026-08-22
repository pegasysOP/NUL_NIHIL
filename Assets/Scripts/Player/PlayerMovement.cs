using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler input;

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 80f;
    public float deceleration = 130f;
    public float airControlMult = 0.85f;
    public float stickDeadzone = 0.3f;

    [Header("Jump")]
    public float jumpForce = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Gravity")]
    public float gravity = -36f;
    public float maxFallSpeed = -20f;
    public float jumpCutMult = 3f;

    [Header("Ground")]
    // includes OneWay, but Player <-> OneWay solver collision is OFF in the matrix for now:
    // physics ignores the matrix, so the ground casts still hit one-way tiles and the
    // ground snap holds us on top - meaning one way platforms accidentally work (not
    // including drop through)
    public LayerMask groundLayer;
    public float maxSlopeAngle = 45f;
    public float skinWidth = 0.02f;

    private Rigidbody2D rb;
    private BoxCollider2D box;

    private Vector2 velocity;
    private Vector2 moveInput;
    private bool jumpHeld;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isGrounded = false;
    private bool wasGrounded = false;
    private Vector2 groundNormal = Vector2.up;
    private float groundProbe;
    private bool frozen;

    // body centre in world space (the pivot sits at the feet)
    public Vector2 Center =>
        box != null ? (Vector2)transform.position + box.offset : (Vector2)transform.position;

    // how far the ground can fall away in one max-speed step on the steepest walkable slope
    private float SnapDistance => maxSpeed * Time.fixedDeltaTime * Mathf.Tan(maxSlopeAngle * Mathf.Deg2Rad) + skinWidth * 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();

        // wire the input handler if the reference is missing, same as rb/box
        if (input == null)
            input = GetComponent<PlayerInputHandler>();

        if (input == null)
            Debug.LogError("PlayerMovement: no PlayerInputHandler assigned or on this object.", this);
    }

    // room transitions freeze movement mid-frame but internal velocity survives so
    // motion resumes exactly where it left off
    public void SetFrozen(bool value)
    {
        if (frozen == value)
            return;

        frozen = value;

        input.SetInputEnabled(!value);

        rb.simulated = !value;
        rb.linearVelocity = value ? Vector2.zero : velocity;
    }

    private void Update()
    {
        if (frozen)
            return;

        // full speed or nothing
        Vector2 raw = input.MoveInput;
        moveInput.x = Mathf.Abs(raw.x) > stickDeadzone ? Mathf.Sign(raw.x) : 0f;
        moveInput.y = raw.y;

        jumpHeld = input.JumpHeld;

        if (input.JumpPressed)
            jumpBufferTimer = jumpBufferTime;

        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (frozen)
            return;

        // adopt what the solver actually did last step, but only where it removed
        // speed (walls/ceilings) - slope contacts can add sideways speed we don't want
        Vector2 solved = rb.linearVelocity;
        if (!isGrounded)
        {
            if (Mathf.Abs(solved.x) < Mathf.Abs(velocity.x))
                velocity.x = solved.x;
            if (Mathf.Abs(solved.y) < Mathf.Abs(velocity.y))
                velocity.y = solved.y;
        }

        wasGrounded = isGrounded;
        CheckGrounded();

        if (isGrounded)
            coyoteTimer = coyoteTime;

        TryJump();

        if (isGrounded)
            MoveAlongGround();
        else
            MoveInAir();

        rb.linearVelocity = velocity;
    }

    private void TryJump()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            return;

        velocity.y = jumpForce;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
    }

    private void CheckGrounded()
    {
        // probe further while grounded so ramps and crests can't cause a hop up,
        // and a full step ahead while falling so landings resolve here instead of
        // in the solver (frictionless contact would skid us down slopes)
        groundProbe = wasGrounded ? SnapDistance : skinWidth * 2f;
        if (!wasGrounded && velocity.y < 0f)
            groundProbe = -velocity.y * Time.fixedDeltaTime + skinWidth * 2f;

        RaycastHit2D hit = CastColliderDown(rb.position, groundProbe);

        // don't re-attach while rising off the ground at the start of a jump
        bool rising = !wasGrounded && velocity.y > 0f;

        isGrounded = hit && IsWalkable(hit.normal) && !rising;
        groundNormal = isGrounded ? hit.normal : Vector2.up;
    }

    // small tolerance so ground exactly at maxSlopeAngle can't flicker unwalkable
    private bool IsWalkable(Vector2 normal)
        => Vector2.Angle(normal, Vector2.up) <= maxSlopeAngle + 1f;

    private void MoveAlongGround()
    {
        float targetSpeed = moveInput.x * maxSpeed;
        float accel = Mathf.Abs(targetSpeed) > Mathf.Epsilon ? acceleration : deceleration;

        // speed is purely horizontal, landing never converts fall momentum into the slope
        float speed = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);

        // cast down over where this step ends and aim the velocity at the resting
        // spot there, so ramps and crests don't cause a hop up
        Vector2 lookAhead = rb.position + Vector2.right * (speed * Time.fixedDeltaTime);
        RaycastHit2D hit = CastColliderDown(lookAhead + Vector2.up * SnapDistance, SnapDistance + groundProbe);

        // distance 0 means wall
        if (hit && hit.distance > 0f && IsWalkable(hit.normal))
        {
            // skinWidth lift keeps the shrunk cast from wedging the collider into the slope
            Vector2 restingPos = lookAhead + Vector2.up * (SnapDistance - hit.distance + skinWidth);
            velocity = (restingPos - rb.position) / Time.fixedDeltaTime;
            groundNormal = hit.normal;
        }
        else
        {
            // walked off a ledge or into too-steep ground; keep our momentum so
            // running off a downslope or ramp lip arcs away naturally
            velocity = new Vector2(speed, velocity.y);
        }
    }

    private void MoveInAir()
    {
        float targetSpeed = moveInput.x * maxSpeed;
        float accel = Mathf.Abs(targetSpeed) > Mathf.Epsilon ? acceleration : deceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * airControlMult * Time.fixedDeltaTime);

        // apply gravity
        float gravityStep = gravity;

        if (velocity.y > 0f && !jumpHeld)
            gravityStep *= jumpCutMult;

        velocity.y += gravityStep * Time.fixedDeltaTime;

        if (velocity.y < maxFallSpeed)
            velocity.y = maxFallSpeed;
    }

    private RaycastHit2D CastColliderDown(Vector2 position, float distance)
    {
        // cast the collider's full footprint (box plus edge radius), shrunk slight
        // so wall grazes don't count as ground
        Vector2 origin = position + box.offset;
        Vector2 size = box.size + Vector2.one * (box.edgeRadius * 2f - skinWidth * 2f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, distance + skinWidth, groundLayer);
    }
}
