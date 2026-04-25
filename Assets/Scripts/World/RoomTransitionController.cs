using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class RoomTransitionController : MonoBehaviour
{
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField, Min(0.01f)] private float scrollDuration = 0.4f;

    public bool IsTransitioning { get; private set; }

    public void BeginTransition(Room from, Room to, Action onComplete)
    {
        if (to == null)
        {
            onComplete?.Invoke();
            return;
        }
        // no source room or no destination bounds — snap with no slide
        if (from == null || to.CameraBounds == null)
        {
            if (to.CameraBounds != null) cameraRig.SetBounds(to.CameraBounds);
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(TransitionRoutine(from, to, onComplete));
    }

    private IEnumerator TransitionRoutine(Room from, Room to, Action onComplete)
    {
        IsTransitioning = true;

        Camera cam = cameraRig.OutputCamera;
        CinemachineBrain brain = cameraRig.Brain;

        bool savedInput = playerInput != null && playerInput.IsInputEnabled;
        Vector2 savedVelocity = playerRigidbody != null ? playerRigidbody.linearVelocity : Vector2.zero;
        bool savedSimulated = playerRigidbody == null || playerRigidbody.simulated;

        if (playerInput != null) playerInput.SetInputEnabled(false);
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.simulated = false;
        }
        // disabling the Brain decouples the Camera from the vcam — we drive it
        // directly during the slide, then sync the vcam back at the end
        if (brain != null) brain.enabled = false;

        // try/finally so brain/physics/input always restore — even if the
        // coroutine is disposed mid-slide (GameObject disabled, scene unloaded)
        try
        {
            Vector3 startPos = cam != null ? cam.transform.position : cameraRig.CameraPosition;
            Vector3 targetPos = ComputeSlideTarget(from, to, startPos);

            float t = 0f;
            while (t < scrollDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / scrollDuration));
                if (cam != null) cam.transform.position = Vector3.LerpUnclamped(startPos, targetPos, k);
                yield return null;
            }
            if (cam != null) cam.transform.position = targetPos;

            cameraRig.SetBounds(to.CameraBounds);
            var vcam = cameraRig.Vcam;
            if (vcam != null)
            {
                Quaternion rot = cam != null ? cam.transform.rotation : Quaternion.identity;
                vcam.ForceCameraPosition(targetPos, rot);
                vcam.PreviousStateIsValid = false;
            }
        }
        finally
        {
            if (brain != null) brain.enabled = true;
            if (playerRigidbody != null)
            {
                playerRigidbody.simulated = savedSimulated;
                playerRigidbody.linearVelocity = savedVelocity;
            }
            if (playerInput != null) playerInput.SetInputEnabled(savedInput);
            IsTransitioning = false;
        }

        onComplete?.Invoke();
    }

    private static Vector3 ComputeSlideTarget(Room from, Room to, Vector3 startPos)
    {
        Bounds toBounds = to.CameraBounds.bounds;
        Vector2 toCenter = toBounds.center;
        Vector2 fromCenter = from != null && from.CameraBounds != null
            ? (Vector2)from.CameraBounds.bounds.center
            : (Vector2)startPos;

        bool horizontal = IsHorizontalTransition(from, to);
        float dirSign = horizontal
            ? Mathf.Sign(toCenter.x - fromCenter.x)
            : Mathf.Sign(toCenter.y - fromCenter.y);
        if (dirSign == 0f) dirSign = 1f;

        float xRaw = horizontal ? startPos.x + ViewportConfig.Width * dirSign : startPos.x;
        float yRaw = horizontal ? startPos.y : startPos.y + ViewportConfig.Height * dirSign;

        float xMin = toBounds.min.x + ViewportConfig.HalfWidth;
        float xMax = toBounds.max.x - ViewportConfig.HalfWidth;
        float yMin = toBounds.min.y + ViewportConfig.HalfHeight;
        float yMax = toBounds.max.y - ViewportConfig.HalfHeight;

        return new Vector3(
            ClampOrCenter(xRaw, xMin, xMax),
            ClampOrCenter(yRaw, yMin, yMax),
            startPos.z);
    }

    // seam direction from the rooms' geometric overlap rather than centers:
    // the axis with the larger overlap is parallel to the seam line, so the
    // transition runs perpendicular to it
    private static bool IsHorizontalTransition(Room from, Room to)
    {
        if (from == null || from.CameraBounds == null || to.CameraBounds == null)
        {
            Vector2 dCenter = to.Center - (from != null ? from.Center : (Vector2)to.Center);
            return Mathf.Abs(dCenter.x) >= Mathf.Abs(dCenter.y);
        }
        Bounds a = from.CameraBounds.bounds;
        Bounds b = to.CameraBounds.bounds;
        float xOverlap = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        float yOverlap = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
        if (xOverlap < 0f && yOverlap < 0f)
        {
            // no shared seam — fall back to center delta
            Vector2 d = (Vector2)b.center - (Vector2)a.center;
            return Mathf.Abs(d.x) >= Mathf.Abs(d.y);
        }
        return yOverlap >= xOverlap;
    }

    private static float ClampOrCenter(float value, float min, float max)
        => min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
}
