using Unity.Cinemachine;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Camera outputCamera;

    public CinemachineCamera Vcam => vcam;
    public Camera OutputCamera => outputCamera != null ? outputCamera : Camera.main;
    public CinemachineBrain Brain =>
        brain != null ? brain : (OutputCamera != null ? OutputCamera.GetComponent<CinemachineBrain>() : null);

    public Vector3 CameraPosition
    {
        get
        {
            var cam = OutputCamera;
            return cam != null ? cam.transform.position : transform.position;
        }
    }

    public void SetFollow(Transform target)
    {
        if (vcam != null) vcam.Follow = target;
    }

    public void SetBounds(Collider2D shape)
    {
        if (confiner == null) return;
        confiner.BoundingShape2D = shape;
        confiner.InvalidateBoundingShapeCache();
    }
}
