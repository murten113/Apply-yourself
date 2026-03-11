using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerCameraLook : MonoBehaviour
{
    public Transform playerBody;
    public float sensitivity = 150f;

    private float xRotation = 0f;
    private Vector2 lookInput;

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}