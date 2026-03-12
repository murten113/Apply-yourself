using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class LookingAround : MonoBehaviour
{
    //Camera
    public Camera MainCamera;

    //Grant access to the InputAsset
    public InputActionAsset InputActions;

    //Map what action is used (input)
    public InputAction LookAround;

    //the vector the stick is moving
    private Vector2 lookVector;

    //Camera sensitivity/ how fast the player can look around
    public float lookspeed = 5f;

    // Track angles separately
    private float yaw = 0f;   // left/right
    private float pitch = 0f; // up/down

    private void OnEnable()
    {
        //When the player is spawned connect all possible inputs in the "Player" catagory
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        //disable actions if this character is switched off or destroyed
        InputActions.FindActionMap("Player").Disable();
    }

    private void Awake()
    {
        LookAround = InputSystem.actions.FindAction("TestInput");

    }

    private void Update()
    {
        lookVector = LookAround.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Rotating();
    }

    private void Rotating()
    {
        yaw += lookVector.x * lookspeed * Time.deltaTime;
        pitch -= lookVector.y * lookspeed * Time.deltaTime;

        // Clamp pitch so the camera can't flip upside down
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Apply rotations independently, Y axis first then X - no tilt possible
        MainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
