using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Camera _playerCamera;
    public float mouseSensitivity = 2f;
    private float _xRotation = 0f;

    private CharacterController _controller;

    public NetworkVariable<FixedString64Bytes> walletAddress = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public TMP_Text usernameText;

    private bool _cameraRotationEnabled = true;

    private void Awake()
    {
        if (usernameText == null)
        {
            // Find the TMP_Text in children named "Username"
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name.ToLower().Contains("user"))
                {
                    usernameText = text;
                    break;
                }
            }
            if (usernameText == null)
            {
                usernameText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        _controller = GetComponent<CharacterController>();
        // Find camera
        _playerCamera = GetComponentInChildren<Camera>(true);
        if (IsOwner && _playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(true);
        }
        else if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(false);
        }

        walletAddress.OnValueChanged += OnAddressChanged;
        if (usernameText != null)
        {
            usernameText.text = FormatAddress(walletAddress.Value.ToString());
        }
    }

    private void OnAddressChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        if (usernameText != null)
        {
            usernameText.text = FormatAddress(curr.ToString());
        }
    }

    private string FormatAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 8) return address;
        return $"{address.Substring(0, 5)}...{address.Substring(address.Length - 3)}";
    }

    public override void OnDestroy()
    {
        walletAddress.OnValueChanged -= OnAddressChanged;
    }

    private void Start()
    {
        if (usernameText != null)
        {
            usernameText.text = FormatAddress(walletAddress.Value.ToString());
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        // Mouse look (FPP) - only if camera rotation is enabled
        if (_cameraRotationEnabled)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            if (_playerCamera != null)
            {
                _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
            transform.Rotate(Vector3.up * mouseX);
        }

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

    [ServerRpc]
    public void SetAddressServerRpc(string address, ServerRpcParams rpcParams = default)
    {
        // Only allow the owner client to set their own address
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;
        walletAddress.Value = address;
    }

    public void SetCameraRotationEnabled(bool enabled)
    {
        _cameraRotationEnabled = enabled;
    }
}
