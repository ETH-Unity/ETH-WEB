using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

// PlayerController handles player movement, camera, and wallet address display (HUD and 3D overhead)
public class PlayerController : NetworkBehaviour
{
    // Movement and camera
    public float moveSpeed = 5f;
    private Camera _playerCamera;
    public float mouseSensitivity = 2f;
    private float _xRotation = 0f;
    private CharacterController _controller;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;


    public NetworkVariable<FixedString64Bytes> walletAddress = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] public TMP_Text usernameText, balanceText, overheadNameText;

    private bool _cameraRotationEnabled = true;

    private void Awake()
    {
        if (usernameText == null)
        {
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
        _playerCamera = GetComponentInChildren<Camera>(true);
        if (IsOwner && _playerCamera != null)
            _playerCamera.gameObject.SetActive(true);
        else if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(false);

        walletAddress.OnValueChanged += OnAddressChanged;
        // Set initial UI
        string formatted = FormatAddress(walletAddress.Value.ToString());
        if (IsOwner && usernameText != null)
            usernameText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;
    }

    private void OnAddressChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        string formatted = FormatAddress(curr.ToString());
        if (IsOwner && usernameText != null)
            usernameText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;
    }

    private string FormatAddress(string address)
    {
        // Shorten address for display (0x123...abc)
        if (string.IsNullOrEmpty(address) || address.Length < 8) return address;
        return $"{address.Substring(0, 5)}...{address.Substring(address.Length - 3)}";
    }

    public override void OnDestroy()
    {
        walletAddress.OnValueChanged -= OnAddressChanged;
    }

    private void Start()
    {
        string formatted = FormatAddress(walletAddress.Value.ToString());
        if (IsOwner && usernameText != null)
            usernameText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;
    }

    void Update()
    {
        if (!IsOwner)
            return;
    
        // Mouse look
        if (_cameraRotationEnabled)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            if (_playerCamera != null)
                _playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
    
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
    
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);
        moveDirection = transform.TransformDirection(moveDirection);
        moveDirection.Normalize();
    
        // Gravity
        if (_controller != null && !_controller.isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else if (_controller != null && _controller.isGrounded)
        {
            verticalVelocity = -1f; // pieni painovoima pitää pelaajan maassa
        }
    
        Vector3 finalMove = moveDirection * moveSpeed + Vector3.up * verticalVelocity;
    
        if (_controller != null)
            _controller.Move(finalMove * Time.deltaTime);
        else
            transform.Translate(finalMove * Time.deltaTime, Space.World);
    }


    // Only the owner can set their own address
    [ServerRpc]
    public void SetAddressServerRpc(string address, ServerRpcParams rpcParams = default)
    {
        if (OwnerClientId != rpcParams.Receive.SenderClientId) return;
        walletAddress.Value = address;
    }

    public void SetCameraRotationEnabled(bool enabled)
    {
        _cameraRotationEnabled = enabled;
    }

    // Update HUD and overhead name
    public void SetAddressHUD(string address)
    {
        if (IsOwner && usernameText != null)
            usernameText.text = FormatAddress(address);
        if (overheadNameText != null)
            overheadNameText.text = FormatAddress(address);
    }

    public void SetBalanceHUD(string balance)
    {
        if (IsOwner && balanceText != null)
            balanceText.text = balance;
    }
}
