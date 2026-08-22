using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class RoomTransitionController : MonoBehaviour
{
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private PlayerMovement player;
    [SerializeField, Min(0.01f)] private float scrollDuration = 0.4f;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        // find the player if the reference is missing
        if (player == null)
            player = FindAnyObjectByType<PlayerMovement>();
    }

    public void BeginTransition(Room from, Room to, Action onComplete)
    {
        if (to == null || to.Bounds == null)
        {
            onComplete?.Invoke();
            return;
        }

        // no source room - snap with no slide
        if (from == null)
        {
            cameraRig.SetBounds(to.Bounds);
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(TransitionRoutine(to, onComplete));
    }

    private IEnumerator TransitionRoutine(Room to, Action onComplete)
    {
        IsTransitioning = true;

        Camera cam = cameraRig.OutputCamera;
        CinemachineBrain brain = cameraRig.Brain;

        if (player != null)
            player.SetFrozen(true);

        // disabling the Brain decouples the Camera from the vcam - drive it
        // directly during the slide, then sync the vcam back at the end
        if (brain != null)
            brain.enabled = false;

        // try/finally so brain/player always restore - even if the coroutine
        // is disposed mid-slide (GameObject disabled, scene unloaded)
        try
        {
            Vector3 startPos = cam != null ? cam.transform.position : cameraRig.CameraPosition;
            // slide to the exact spot the confiner will hold for the frozen
            // player, so the handoff back to Cinemachine can't jerk
            Vector2 target = player != null ? player.Center : (Vector2)startPos;
            Vector3 endPos = ConfinedCameraPosition(target, to.Bounds.bounds, startPos.z);

            float t = 0f;
            while (t < scrollDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / scrollDuration));
                if (cam != null)
                    cam.transform.position = Vector3.LerpUnclamped(startPos, endPos, k);

                yield return null;
            }

            if (cam != null)
                cam.transform.position = endPos;

            cameraRig.SetBounds(to.Bounds);
            CinemachineCamera vcam = cameraRig.Vcam;
            if (vcam != null)
            {
                Quaternion rot = cam != null ? cam.transform.rotation : Quaternion.identity;
                vcam.ForceCameraPosition(endPos, rot);
            }
        }
        finally
        {
            if (brain != null)
                brain.enabled = true;

            if (player != null)
                player.SetFrozen(false);

            IsTransitioning = false;
        }

        onComplete?.Invoke();
    }

    // where a camera confined to bounds rests while tracking target
    private static Vector3 ConfinedCameraPosition(Vector2 target, Bounds bounds, float z)
    {
        return new Vector3(
            ClampOrCenter(target.x, bounds.min.x + ViewportConfig.HalfWidth, bounds.max.x - ViewportConfig.HalfWidth),
            ClampOrCenter(target.y, bounds.min.y + ViewportConfig.HalfHeight, bounds.max.y - ViewportConfig.HalfHeight),
            z);
    }

    private static float ClampOrCenter(float value, float min, float max)
        => min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
}
