using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// Handles user-to-user contract interactions, including sending and signing transfers.

public class UserUser : MonoBehaviour
{
    [Header("Contract Settings")]
    [Tooltip("The deployed UserUser contract address")]
    [SerializeField] private string contractAddress = "";

    [Header("UI References")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button signTransferButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private PlayerListUI playerListUI;
    [SerializeField] private TMP_Text selectedRecipientText;
    [SerializeField] private TMP_Text receivedMessageText;

    private string _selectedRecipientAddress;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponentInParent<PlayerController>();
        // Set contract address from config if not set in inspector
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserUserContractAddress;
        }
    }

   
    /// Prepares the UI and event listeners for the local player.
    public void InitializeForOwner()
    {
        gameObject.SetActive(true);
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);
        if (signTransferButton != null)
        {
            signTransferButton.onClick.AddListener(OnSignTransferClicked);
            signTransferButton.gameObject.SetActive(false);
        }
        if (playerListUI == null)
        {
            Debug.LogError("PlayerListUI is not assigned in UserUser script. Please assign it in the Inspector.");
        }
        else
        {
            if (playerListUI.gameObject.GetComponentInParent<PlayerController>() == GetComponentInParent<PlayerController>())
                playerListUI.gameObject.SetActive(true);
            playerListUI.OnAddressSelected += HandleRecipientSelected;
            playerListUI.RefreshPlayerList();
        }
        UpdateSelectedRecipientText();
    }

    /// Shows the sign transfer button.
    public void EnableSignButton()
    {
        if (signTransferButton != null)
            signTransferButton.gameObject.SetActive(true);
    }

    /// Disables UI and input for non-owners.
    public void DisableForNonOwner()
    {
        if (playerListUI != null)
        {
            playerListUI.OnAddressSelected -= HandleRecipientSelected;
            if (playerListUI.gameObject.GetComponentInParent<PlayerController>() == GetComponentInParent<PlayerController>())
                playerListUI.gameObject.SetActive(false);
        }
        if(amountInput) amountInput.interactable = false;
        if(messageInput) messageInput.interactable = false;
        if(sendButton) sendButton.interactable = false;
        if(signTransferButton) signTransferButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (playerListUI != null)
            playerListUI.OnAddressSelected -= HandleRecipientSelected;
    }

    private void HandleRecipientSelected(string address)
    {
        _selectedRecipientAddress = address;
        if (statusText != null) statusText.text = "Recipient selected.";
        UpdateSelectedRecipientText();
    }

    private void UpdateSelectedRecipientText()
    {
        if (selectedRecipientText != null)
        {
            if (!string.IsNullOrEmpty(_selectedRecipientAddress))
                selectedRecipientText.text = $"To: {FormatAddress(_selectedRecipientAddress)}";
            else
                selectedRecipientText.text = "To: (No recipient selected)";
        }
    }

    /// Called when the send button is clicked. Initiates a transfer.
    public void OnSendClicked()
    {
        // Ensure contract address is set from config if empty
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserUserContractAddress;
        }
        if (string.IsNullOrEmpty(_selectedRecipientAddress))
        {
            if (statusText != null)
                statusText.text = "Please select a recipient from the list.";
            return;
        }
        if (amountInput == null || string.IsNullOrEmpty(amountInput.text))
        {
            if (statusText != null)
                statusText.text = "Please enter an amount.";
            return;
        }
        string amountWei = (System.Numerics.BigInteger.Parse(amountInput.text) * 1000000000000000000).ToString();
        if (string.IsNullOrEmpty(amountWei) || amountWei == "0")
        {
            if (statusText != null)
                statusText.text = "Invalid amount.";
            return;
        }
        string message = messageInput != null ? messageInput.text : "";
        if (statusText != null)
            statusText.text = $"Initiating transfer to {_selectedRecipientAddress}...";
        MetaMaskInterop.InitiateTransfer(_selectedRecipientAddress, amountWei, message, contractAddress);
        // Notification is now sent after MetaMask confirms transaction, not here.
    }

    /// Called when the sign transfer button is clicked. Signs the transfer.
    public void OnSignTransferClicked()
    {
        // Ensure contract address is set from config if empty
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserUserContractAddress;
        }
        if (string.IsNullOrEmpty(contractAddress))
        {
            if (statusText != null)
                statusText.text = "Contract address not set.";
            return;
        }
        if (statusText != null)
            statusText.text = "Signing transfer...";
        MetaMaskInterop.SignTransfer(contractAddress);
        if (receivedMessageText != null)
            receivedMessageText.text = "";
        if (_playerController != null)
            _playerController.ClearNotification();
        if (signTransferButton != null)
            signTransferButton.gameObject.SetActive(false);
    }

    // --- SignTransfer Callback Handlers ---

    public void OnSignTransferSubmitted(string txHash)
    {
        if (statusText != null) statusText.text = $"Sign transaction submitted: {FormatAddress(txHash, 6, 0)}...";
    }

    public void OnSignTransferConfirmed(string txHash)
    {
        if (statusText != null) statusText.text = $"Sign transaction confirmed: {FormatAddress(txHash, 6, 0)}...";
    }

    public void OnSignTransferError(string error)
    {
        if (statusText != null) statusText.text = $"Sign transfer error: {error}";
        Debug.LogError($"UserUser: SignTransferError: {error}");
    }

    public void OnSignedMessageReceived(string message)
    {
        if (statusText != null) statusText.text = $"Transfer signed! Message retrieved.";
        Debug.Log($"UserUser: Signed Message Received: {message}");
        if (receivedMessageText != null)
            receivedMessageText.text = $"Message: {message}";
    }

    public void OnSignedMessageError(string error)
    {
        if (statusText != null) statusText.text = $"Error retrieving message: {error}";
        if (receivedMessageText != null) receivedMessageText.text = "Could not retrieve message.";
        Debug.LogError($"UserUser: SignedMessageError: {error}");
    }

    /// Converts an ETH string to a wei string.
    private string EthToWei(string ethString)
    {
        if (decimal.TryParse(ethString, out decimal eth))
        {
            decimal wei = eth * 1000000000000000000m;
            return ((System.Numerics.BigInteger)wei).ToString();
        }
        return "0";
    }

    /// Shortens an address for display (e.g., 0x123...abc).
    private string FormatAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 8) return address;
        return $"{address.Substring(0, 5)}...{address.Substring(address.Length - 3)}";
    }

    /// Formats a string with a prefix and suffix length (for hashes, etc).
    private string FormatAddress(string text, int prefixLength, int suffixLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length < (prefixLength + suffixLength)) return text;
        if (suffixLength == 0) return $"{text.Substring(0, prefixLength)}";
        return $"{text.Substring(0, prefixLength)}...{text.Substring(text.Length - suffixLength)}";
    }

    // --- SignTransfer Callback Handlers ---

    public void OnInitiateTransferSubmitted(string txHash)
    {
        if (statusText != null)
            statusText.text = $"Transfer transaction submitted: {FormatAddress(txHash, 6, 0)}...";
    }

    /// Notifies the recipient after MetaMask confirms the transaction.
    public void NotifyRecipientAfterTransfer()
    {
        PlayerController localPlayerController = GetComponentInParent<PlayerController>();
        if (localPlayerController != null && localPlayerController.IsOwner && !string.IsNullOrEmpty(_selectedRecipientAddress))
        {
            localPlayerController.SendTransferNotification(_selectedRecipientAddress);
        }
        else
        {
            Debug.LogWarning("[UserUser] Could not notify recipient after transfer. No local PlayerController or recipient address.");
        }
    }
}
