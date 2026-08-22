using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomId;
    // auto wired from this object, right click the component -> Fit Bounds To Tiles
    [SerializeField] private BoxCollider2D bounds;

    public string RoomId => string.IsNullOrEmpty(roomId) ? name : roomId;
    public BoxCollider2D Bounds => bounds != null ? bounds : (bounds = GetComponent<BoxCollider2D>());

    public Vector2 Center => Bounds != null ? (Vector2)Bounds.bounds.center : (Vector2)transform.position;

    public event Action OnPlayerEntered;
    public event Action OnPlayerExited;

    internal void NotifyPlayerEntered() => OnPlayerEntered?.Invoke();
    internal void NotifyPlayerExited() => OnPlayerExited?.Invoke();

    public bool ContainsPoint(Vector2 worldPoint)
    {
        return Bounds != null && Bounds.OverlapPoint(worldPoint);
    }

    private void OnEnable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.Unregister(this);
    }

    private void OnDrawGizmos()
    {
        if (Bounds != null)
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
            Bounds b = Bounds.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }

#if UNITY_EDITOR
    // 16 PPU
    private const float PixelSize = 1f / 16f;

    // hug the painted tiles, snapped to whole tiles so adjacent rooms share
    // seam edges exactly (the room handoff polls a point against these boxes)
    [ContextMenu("Fit Bounds To Tiles")]
    private void FitBoundsToTiles()
    {
        Bounds? world = null;
        foreach (Tilemap tilemap in GetComponentsInChildren<Tilemap>())
        {
            tilemap.CompressBounds();
            if (tilemap.cellBounds.size.x <= 0 || tilemap.cellBounds.size.y <= 0)
                continue;

            Bounds lb = tilemap.localBounds;
            Bounds wb = new Bounds(tilemap.transform.TransformPoint(lb.center), lb.size);
            if (world.HasValue)
            {
                Bounds u = world.Value;
                u.Encapsulate(wb);
                world = u;
            }
            else
            {
                world = wb;
            }
        }

        if (!world.HasValue)
        {
            Debug.LogWarning($"Room '{name}': no painted tiles found to fit bounds to.", this);
            return;
        }

        BoxCollider2D box = Bounds;
        UnityEditor.Undo.RecordObject(box, "Fit Room Bounds To Tiles");
        Vector2 min = new Vector2(Mathf.Round(world.Value.min.x), Mathf.Round(world.Value.min.y));
        Vector2 max = new Vector2(Mathf.Round(world.Value.max.x), Mathf.Round(world.Value.max.y));
        box.offset = (min + max) * 0.5f - (Vector2)transform.position;
        box.size = max - min;
    }

    private void OnValidate()
    {
        // defer so Unity doesn't warn about SendMessage during serialization
        UnityEditor.EditorApplication.delayCall += DeferredValidate;
    }

    private void DeferredValidate()
    {
        if (this == null)
            return;

        BoxCollider2D box = Bounds;
        if (box != null && !box.isTrigger)
            box.isTrigger = true;

        SnapTransformToPixel(transform);
        SnapBox(box);

        if (box != null)
        {
            Vector3 size = box.bounds.size;
            if (size.x + 1e-4f < ViewportConfig.Width || size.y + 1e-4f < ViewportConfig.Height)
            {
                Debug.LogWarning(
                    $"Room '{name}': Bounds is {size.x:0.###} x {size.y:0.###}, " +
                    $"smaller than the viewport ({ViewportConfig.Width} x {ViewportConfig.Height}). " +
                    "The camera will show past the room edges.",
                    this);
            }
        }
    }

    private static void SnapTransformToPixel(Transform t)
    {
        Vector3 p = t.position;
        p.x = Mathf.Round(p.x / PixelSize) * PixelSize;
        p.y = Mathf.Round(p.y / PixelSize) * PixelSize;
        p.z = 0f;
        t.position = p;
    }

    private static void SnapBox(BoxCollider2D box)
    {
        if (box == null)
            return;

        Vector2 s = box.size;
        s.x = Mathf.Max(PixelSize, Mathf.Round(s.x / PixelSize) * PixelSize);
        s.y = Mathf.Max(PixelSize, Mathf.Round(s.y / PixelSize) * PixelSize);
        box.size = s;

        Vector2 o = box.offset;
        o.x = Mathf.Round(o.x / PixelSize) * PixelSize;
        o.y = Mathf.Round(o.y / PixelSize) * PixelSize;
        box.offset = o;
    }
#endif
}
