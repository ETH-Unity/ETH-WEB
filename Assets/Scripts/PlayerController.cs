using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

/// Handles player movement, camera, wallet address display, and transfer notifications.
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

    [SerializeField] public TMP_Text addressText, balanceText, overheadNameText, notificationText;

    private bool _cameraRotationEnabled = true;
    public bool IsMenuOpen { get; set; } = false;

    private void Awake()
    {
        if (addressText == null)
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.gameObject.name.ToLower().Contains("user"))
                {
                    addressText = text;
                    break;
                }
            }
            if (addressText == null)
                addressText = GetComponentInChildren<TMP_Text>(true);
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
        string formatted = FormatAddress(walletAddress.Value.ToString());
        if (IsOwner && addressText != null)
            addressText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;

        // UI Ownership Control
        var walletLogin = GetComponentInChildren<WalletLogin>(true);
        if (walletLogin != null)
        {
            if (IsOwner)
                walletLogin.InitializeForOwner();
            else
                walletLogin.DisableForNonOwner();
        }
        var userUser = GetComponentInChildren<UserUser>(true);
        if (userUser != null)
        {
            if (IsOwner)
                userUser.InitializeForOwner();
            else
                userUser.DisableForNonOwner();
        }
    }

    private void OnAddressChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        string formatted = FormatAddress(curr.ToString());
        if (IsOwner && addressText != null)
            addressText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;
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
        string formatted = FormatAddress(walletAddress.Value.ToString());
        if (IsOwner && addressText != null)
            addressText.text = formatted;
        if (overheadNameText != null)
            overheadNameText.text = formatted;
    }

    void Update()
    {
        if (!IsOwner)
            return;
        if (IsMenuOpen)
            return;
        // Mouse look (FPP) - only if camera rotation is enabled
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
        moveDirection.Normalize();
        if (_controller != null)
            _controller.Move(transform.TransformDirection(moveDirection) * moveSpeed * Time.deltaTime);
        else
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
    }

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

    public void SetAddressHUD(string address)
    {
        if (IsOwner && addressText != null)
            addressText.text = FormatAddress(address);
        if (overheadNameText != null)
            overheadNameText.text = FormatAddress(address);
    }

    public void SetBalanceHUD(string balance)
    {
        if (IsOwner && balanceText != null)
            balanceText.text = balance;
    }

    // --- Transfer Notification RPCs ---
    // Called by UserUser.cs after initiating a transfer. Notifies the recipient.
    public void SendTransferNotification(string recipientAddress)
    {
        if (!IsOwner) return;
        NotifyRecipientServerRpc(recipientAddress);
    }

    [ServerRpc]
    private void NotifyRecipientServerRpc(string recipientAddress)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.walletAddress.Value.ToString().Equals(recipientAddress, System.StringComparison.OrdinalIgnoreCase))
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { player.OwnerClientId }
                    }
                };
                player.ReceiveTransferNotificationClientRpc(clientRpcParams);
                Debug.Log($"[Server] Sent transfer notification to ClientId {player.OwnerClientId} with address {recipientAddress}");
                return;
            }
        }
        Debug.LogWarning($"[Server] NotifyRecipientServerRpc: Could not find a connected player with address {recipientAddress}");
    }

    [ClientRpc]
    private void ReceiveTransferNotificationClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        if (notificationText != null)
            notificationText.text = "You have a pending transfer to sign!";
        var userUser = GetComponentInChildren<UserUser>(true);
        if (userUser != null)
            userUser.EnableSignButton();
    }

    public void ClearNotification()
    {
        if (IsOwner && notificationText != null)
            notificationText.text = "";
    }
}
