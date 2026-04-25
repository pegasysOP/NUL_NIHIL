using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomId;
    [SerializeField] private BoxCollider2D boundsTrigger;
    [SerializeField] private Collider2D cameraBounds;

    public string RoomId => string.IsNullOrEmpty(roomId) ? name : roomId;
    public BoxCollider2D BoundsTrigger => boundsTrigger;
    public Collider2D CameraBounds => cameraBounds;

    public Vector2 Center =>
        boundsTrigger != null ? (Vector2)boundsTrigger.bounds.center : (Vector2)transform.position;

    public event Action OnPlayerEntered;
    public event Action OnPlayerExited;

    internal void NotifyPlayerEntered() => OnPlayerEntered?.Invoke();
    internal void NotifyPlayerExited() => OnPlayerExited?.Invoke();

    public bool ContainsPoint(Vector2 worldPoint)
    {
        return boundsTrigger != null && boundsTrigger.OverlapPoint(worldPoint);
    }

    private void OnEnable()
    {
        if (RoomManager.Instance != null) RoomManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null) RoomManager.Instance.Unregister(this);
    }

    private void OnDrawGizmos()
    {
        if (boundsTrigger != null)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
            var b = boundsTrigger.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
        if (cameraBounds != null)
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
            var b = cameraBounds.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }

#if UNITY_EDITOR
    // 16 PPU
    private const float PixelSize = 1f / 16f;

    private void OnValidate()
    {
        // defer so Unity doesn't warn about SendMessage during serialization
        UnityEditor.EditorApplication.delayCall += DeferredValidate;
    }

    private void DeferredValidate()
    {
        if (this == null) return;

        SnapTransformToPixel(transform);
        SnapBox(boundsTrigger);
        if (cameraBounds is BoxCollider2D cbBox) SnapBox(cbBox);

        if (cameraBounds != null)
        {
            var size = cameraBounds.bounds.size;
            if (size.x + 1e-4f < ViewportConfig.Width || size.y + 1e-4f < ViewportConfig.Height)
            {
                Debug.LogWarning(
                    $"Room '{name}': CameraBounds is {size.x:0.###} x {size.y:0.###}, " +
                    $"smaller than the viewport ({ViewportConfig.Width} x {ViewportConfig.Height}). " +
                    "Cinemachine Confiner2D will zoom the camera in, breaking pixel-perfect.",
                    this);
            }
        }
    }

    private static void SnapTransformToPixel(Transform t)
    {
        var p = t.position;
        p.x = Mathf.Round(p.x / PixelSize) * PixelSize;
        p.y = Mathf.Round(p.y / PixelSize) * PixelSize;
        p.z = 0f;
        t.position = p;
    }

    private static void SnapBox(BoxCollider2D box)
    {
        if (box == null) return;

        var s = box.size;
        s.x = Mathf.Max(PixelSize, Mathf.Round(s.x / PixelSize) * PixelSize);
        s.y = Mathf.Max(PixelSize, Mathf.Round(s.y / PixelSize) * PixelSize);
        box.size = s;

        var o = box.offset;
        o.x = Mathf.Round(o.x / PixelSize) * PixelSize;
        o.y = Mathf.Round(o.y / PixelSize) * PixelSize;
        box.offset = o;
    }
#endif
}
