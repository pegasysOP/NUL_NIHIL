using UnityEngine;


[DisallowMultipleComponent]
public class ServiceCrawler : MonoBehaviour
{
    [Header("Patrol")]
    public float moveSpeed = 2f;
    public LayerMask surfaceLayers;
    public float skinWidth = 0.02f;

    [Header("Damage")]
    public int contactDamage = 20;
    public LayerMask playerLayers;

    private Rigidbody2D rb;
    private BoxCollider2D box;

    private PlayerMovement player;
    private PlayerHealth playerHealth;

    private float direction = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        // TODO: use better approach in future, rooms manage enemies (and spawns) and can give them a reference to the player
        player = FindAnyObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogError("ServiceCrawler: no PlayerMovement in the scene.", this);
        else
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void FixedUpdate()
    {
        // hold still while a room transition freezes the player
        if (player.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // the wall variant is this prefab rotated: local right is the patrol
        // axis and local down points into the surface
        Vector2 travel = (Vector2)transform.right * direction;

        if (ShouldTurn(travel, -transform.up))
            direction = -direction;

        rb.linearVelocity = (Vector2)transform.right * (direction * moveSpeed);
    }

    private bool ShouldTurn(Vector2 travel, Vector2 down)
    {
        float halfLength = box.size.x * 0.5f;
        float halfHeight = box.size.y * 0.5f;

        // blocked ahead
        if (Physics2D.Raycast(rb.position, travel, halfLength + skinWidth * 2f, surfaceLayers))
            return true;

        // surface ends at the leading edge
        Vector2 ledgeOrigin = rb.position + travel * halfLength;
        return !Physics2D.Raycast(ledgeOrigin, down, halfHeight + skinWidth * 2f, surfaceLayers);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // player layers only
        if (((1 << other.gameObject.layer) & playerLayers.value) == 0)
            return;

        playerHealth.TakeDamage(contactDamage, rb.position);
    }
}
