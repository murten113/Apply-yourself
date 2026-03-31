using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Node-based movement: look at a MovementNode and press Interact to move there.
/// Also handles camera look (mouse/joystick).
/// </summary>
public class NodeMovementController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Optional. If null, uses the camera's parent for pitch (or camera if no parent). Add an empty GameObject between player and camera for proper FPS look.")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 0.15f;
    [Tooltip("Extra multiplier for gamepad/joystick (stick returns -1 to 1, needs higher scale)")]
    [SerializeField] private float stickSensitivity = 70f;
    [Tooltip("Overall camera look multiplier (mouse and gamepad). Use 1 for default; raise or lower to tune feel.")]
    [SerializeField] private float lookSpeed = 1f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool lockCursor = true;

    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference interactAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private PublicRaycast publicRaycast;

    private float pitch;
    private float yaw;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    private float moveProgress;
    private bool isMoving;

    public bool IsMoving => isMoving;
    public bool IsLookingAtNode => GetLookedAtNode() != null;

    private void OnEnable()
    {
        lookAction?.action?.Enable();
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        lookAction?.action?.Disable();
        interactAction?.action?.Disable();
    }

    private void Start()
    {
        if (publicRaycast == null)
            publicRaycast = Object.FindFirstObjectByType<PublicRaycast>();

        if (playerCamera == null) playerCamera = Camera.main;
        if (cameraPivot == null && playerCamera != null)
            cameraPivot = playerCamera.transform; // Use camera for pitch when no separate pivot

        pitch = cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f;
        yaw = transform.eulerAngles.y;

        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (isMoving)
        {
            UpdateMovement();
            return;
        }

        UpdateCameraLook();

        if (WasPressed(interactAction) || Input.GetMouseButtonDown(0))
        {
            var node = GetLookedAtNode();
            if (node != null)
                StartMoveTo(node.MoveTarget);
        }
    }

    private void UpdateCameraLook()
    {
        if (cameraPivot == null) return;

        Vector2 look;
        if (lookAction?.action != null)
        {
            look = lookAction.action.ReadValue<Vector2>();
            float scale = stickSensitivity * Time.deltaTime;
            if (look.sqrMagnitude > 1f)
                scale = lookSensitivity;
            look *= scale * lookSpeed;
        }
        else
        {
            // Legacy fallback
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * lookSensitivity * 10f * lookSpeed;
        }

        yaw += look.x;
        pitch += (invertY ? 1 : -1) * look.y;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.eulerAngles = new Vector3(0, yaw, 0);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    private void StartMoveTo(Vector3 target)
    {
        moveStartPos = transform.position;
        moveTargetPos = new Vector3(target.x, transform.position.y, target.z);
        moveProgress = 0f;
        isMoving = true;
    }

    private void UpdateMovement()
    {
        moveProgress += (moveSpeed * Time.deltaTime) / Vector3.Distance(moveStartPos, moveTargetPos);
        if (moveProgress >= 1f)
        {
            transform.position = moveTargetPos;
            isMoving = false;
            return;
        }

        float t = Mathf.SmoothStep(0f, 1f, moveProgress);
        transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, t);
    }

    private MovementNode GetLookedAtNode()
    {
        if (publicRaycast == null) return null;
        // Same as PlayerTools: first Physics.Raycast hit is often level geometry, not the node.
        return publicRaycast.TryGetNearestParentComponent<MovementNode>(out MovementNode node, out _)
            ? node
            : null;
    }

    private bool WasPressed(InputActionReference actionRef)
    {
        return actionRef != null && actionRef.action != null && actionRef.action.WasPressedThisFrame();
    }
}
