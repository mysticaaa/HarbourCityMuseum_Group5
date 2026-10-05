using UnityEngine;

/// <summary>
/// Simple desktop movement controller for testing without VR headset.
/// Uses WASD + mouse look (press-hold right mouse button).
/// Attach to the player root (same level as camera).
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private CharacterController characterController;
    private float pitch = 0f;
    private float yaw = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Start()
    {
        // Lock cursor when running
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * h + transform.forward * v);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            move *= sprintMultiplier;
        }

        characterController.SimpleMove(move * moveSpeed);
    }

    private void HandleLook()
    {
        if (playerCamera == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertY)
        {
            pitch -= mouseY;
        }
        else
        {
            pitch += mouseY;
        }

        pitch = Mathf.Clamp(pitch, -80f, 80f);
        yaw += mouseX;

        // Rotate body around Y, camera around X
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
