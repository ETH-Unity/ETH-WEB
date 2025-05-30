using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Camera _playerCamera;
    public float mouseSensitivity = 2f;
    private float _xRotation = 0f;

    private CharacterController _controller;

    public override void OnNetworkSpawn()
    {
        _controller = GetComponent<CharacterController>();
        // Find or create a Camera as a child
        _playerCamera = GetComponentInChildren<Camera>(true);
        if (IsOwner && _playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(true);
        }
        else if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        // Mouse look (FPP)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        if (_playerCamera != null)
        {
            _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);
        moveDirection.Normalize();

        if (_controller != null)
        {
            _controller.Move(transform.TransformDirection(moveDirection) * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
        }
    }
}
