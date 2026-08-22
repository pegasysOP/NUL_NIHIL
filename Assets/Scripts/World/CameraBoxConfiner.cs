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

        Vector3 pos = state.GetCorrectedPosition();
        Vector2 confined = ConfinedPosition(pos, bounds.Value);
        state.PositionCorrection += new Vector3(confined.x, confined.y, pos.z) - pos;
    }

    // single-screen rooms are 12 tiles tall but the viewport is 11.25, so the
    // vertical clamp tucks inwards by half the difference in every room
    private const float roomUnitHeight = 12f;
    private const float verticalInset = (roomUnitHeight - ViewportConfig.Height) * 0.5f;

    public static Vector2 ConfinedPosition(Vector2 target, Bounds b)
    {
        return new Vector2(
            ClampOrCenter(target.x, b.min.x + ViewportConfig.HalfWidth, b.max.x - ViewportConfig.HalfWidth),
            ClampOrCenter(target.y, b.min.y + ViewportConfig.HalfHeight + verticalInset,
                                    b.max.y - ViewportConfig.HalfHeight - verticalInset));
    }

    // rooms smaller than the clamp range hold their centre line
    private static float ClampOrCenter(float value, float min, float max)
        => min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
}
