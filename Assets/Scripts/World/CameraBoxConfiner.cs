using Unity.Cinemachine;
using UnityEngine;

// rooms are axis-aligned boxes, so a straight clamp replaces Confiner2D:
// its polygon cache rebakes over several frames on every bounds switch,
// leaving the camera unconfined for a moment (visible flicker)
public class CameraBoxConfiner : CinemachineExtension
{
    private Bounds? bounds;

    public void SetBounds(Collider2D shape)
    {
        bounds = shape != null ? shape.bounds : (Bounds?)null;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || bounds == null)
            return;

        Bounds b = bounds.Value;
        Vector3 pos = state.GetCorrectedPosition();
        Vector3 clamped = new Vector3(
            ClampOrCenter(pos.x, b.min.x + ViewportConfig.HalfWidth, b.max.x - ViewportConfig.HalfWidth),
            ClampOrCenter(pos.y, b.min.y + ViewportConfig.HalfHeight, b.max.y - ViewportConfig.HalfHeight),
            pos.z);
        state.PositionCorrection += clamped - pos;
    }

    // rooms exactly one screen tall clamp to their centre line
    private static float ClampOrCenter(float value, float min, float max)
        => min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
}
