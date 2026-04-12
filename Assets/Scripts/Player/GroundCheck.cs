using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public LayerMask GroundLayer;
    public float checkDistance = 0.0625f;
    public float checkWidth = 0.35f;

    public bool IsGrounded { get; private set; }
    public Vector2 GroundNormal { get; private set; } = Vector2.up;

    public void Check()
    {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, new Vector2(checkWidth, checkDistance), 0f, Vector2.down, checkDistance, GroundLayer);
        if (hit)
        {
            IsGrounded = true;
            GroundNormal = hit.normal;
        }
        else
        {
            IsGrounded = false;
            GroundNormal = Vector2.up;

        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.down * (checkDistance / 2), new Vector3(checkWidth, checkDistance, 0f));
    }
}
