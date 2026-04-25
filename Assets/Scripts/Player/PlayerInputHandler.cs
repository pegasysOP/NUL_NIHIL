using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool IsInputEnabled => controls != null && controls.Gameplay.enabled;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();

        controls.Gameplay.Move.performed += OnMove;
        controls.Gameplay.Move.canceled += OnMove;

        controls.Gameplay.Jump.started += OnJumpStarted;
        controls.Gameplay.Jump.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();

        controls.Gameplay.Move.performed -= OnMove;
        controls.Gameplay.Move.canceled -= OnMove;

        controls.Gameplay.Jump.started -= OnJumpStarted;
        controls.Gameplay.Jump.canceled -= OnJumpCanceled;
    }

    private void LateUpdate()
    {
        JumpPressed = false;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (controls == null) return;
        if (enabled)
        {
            controls.Gameplay.Enable();
        }
        else
        {
            controls.Gameplay.Disable();
            MoveInput = Vector2.zero;
            JumpPressed = false;
            JumpHeld = false;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        JumpPressed = true;
        JumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        JumpHeld = false;
    }
}
