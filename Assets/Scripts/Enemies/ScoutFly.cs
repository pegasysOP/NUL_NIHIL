using UnityEngine;


[DisallowMultipleComponent]
public class ScoutFly : MonoBehaviour
{
    [Header("Targeting")]
    public float detectRange = 6f;
    public float loseRange = 9f;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Damage")]
    public int contactDamage = 20;
    public LayerMask playerLayers;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private PlayerMovement player;
    private PlayerHealth playerHealth;

    private bool chasing;
    private Color baseColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseColor = spriteRenderer.color;
    }

    private void Start()
    {
        // TODO: use better approach in future, rooms manage enemies (and spawns) and can give them a reference to the player
        player = FindAnyObjectByType<PlayerMovement>();

        if (player == null)
            Debug.LogError("ScoutFly: no PlayerMovement in the scene.", this);
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

        Vector2 toPlayer = player.Center - rb.position;
        float dist = toPlayer.magnitude;

        chasing = dist <= (chasing ? loseRange : detectRange);

        rb.linearVelocity = chasing && dist > 0.001f ? toPlayer / dist * moveSpeed : Vector2.zero;
    }

    private void LateUpdate()
    {
        // placeholder tell so chasing is visible
        spriteRenderer.color = chasing ? Color.red : baseColor;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // player layers only
        if (((1 << other.gameObject.layer) & playerLayers.value) == 0)
            return;

        playerHealth.TakeDamage(contactDamage, rb.position);
    }
}
