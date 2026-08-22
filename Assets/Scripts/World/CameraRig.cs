using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CameraBoxConfiner confiner;
    [SerializeField] private Camera outputCamera;

    public CinemachineCamera Vcam => vcam;
    public Camera OutputCamera => outputCamera != null ? outputCamera : Camera.main;
    public CinemachineBrain Brain =>
        brain != null ? brain : (OutputCamera != null ? OutputCamera.GetComponent<CinemachineBrain>() : null);

    public Vector3 CameraPosition
    {
        get
        {
            Camera cam = OutputCamera;
            return cam != null ? cam.transform.position : transform.position;
        }
    }

    private void Awake()
    {
        if (vcam != null && Mathf.Abs(vcam.Lens.OrthographicSize - ViewportConfig.HalfHeight) > 0.001f)
            Debug.LogWarning(
                $"CameraRig: vcam lens ortho size {vcam.Lens.OrthographicSize} doesn't match " +
                $"ViewportConfig.HalfHeight {ViewportConfig.HalfHeight}.", this);

        Camera cam = OutputCamera;
        PixelPerfectCamera ppc = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;

        if (ppc != null &&
            (ppc.refResolutionX != ViewportConfig.PixelsWide ||
             ppc.refResolutionY != ViewportConfig.PixelsHigh ||
             ppc.assetsPPU != (int)ViewportConfig.PixelsPerUnit))
            Debug.LogWarning(
                $"CameraRig: Pixel Perfect Camera is {ppc.refResolutionX}x{ppc.refResolutionY} at " +
                $"{ppc.assetsPPU} PPU, ViewportConfig expects {ViewportConfig.PixelsWide}x" +
                $"{ViewportConfig.PixelsHigh} at {ViewportConfig.PixelsPerUnit}.", this);
    }

    public void SetFollow(Transform target)
    {
        if (vcam != null)
            vcam.Follow = target;
    }

    public void SetBounds(Collider2D shape)
    {
        if (confiner == null && vcam != null)
            confiner = vcam.GetComponent<CameraBoxConfiner>();

        if (confiner != null)
            confiner.SetBounds(shape);
    }
}
