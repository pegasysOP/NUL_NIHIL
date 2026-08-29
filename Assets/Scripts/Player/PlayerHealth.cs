using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    [Header("Health")]
    public int maxHealth = 99;
    public int hazardDamage = 25;

    [Header("Damage Response")]
    public float invulnerabilityTime = 1f;
    public LayerMask hazardLayers;

    public int Current { get; private set; }
    public bool IsInvulnerable => invulnTimer > 0f;

    // how far into the current i-frame window we are, for the flash/blink visuals
    public float InvulnerabilityElapsed => invulnerabilityTime - invulnTimer;

    public event Action<int> HealthChanged;

    private float invulnTimer;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        Current = maxHealth;
    }

    private void Update()
    {
        if (movement.IsFrozen)
            return;

        invulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount, Vector2 sourcePoint)
    {
        // invulnerable whilst frozen
        if (movement.IsFrozen)
            return;

        // iframes
        if (invulnTimer > 0f)
            return;

        Current = Mathf.Max(0, Current - amount);
        invulnTimer = invulnerabilityTime;

        movement.ApplyKnockback(sourcePoint);

        HealthChanged?.Invoke(Current);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // hazard layers only
        if (((1 << other.gameObject.layer) & hazardLayers.value) == 0)
            return;

        TakeDamage(hazardDamage, other.ClosestPoint(movement.Center));
    }
}
