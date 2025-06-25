using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;

// Handles per-player wallet login (MetaMask for WebGL),
// wallet address network sync, and balance display.
public class WalletLogin : MonoBehaviour
{
    [SerializeField] private Button metaMaskLoginButton;
    private PlayerController _playerController;
    
    // Store the wallet address for external access
    private string _walletAddress;
    public string WalletAddress => _walletAddress;

    void Awake()
    {
        _playerController = GetComponentInParent<PlayerController>();
    }

    // Prepares the wallet login UI for the local player.
    public void InitializeForOwner()
    {
        gameObject.SetActive(true);
#if UNITY_WEBGL && !UNITY_EDITOR
        string uniqueName = $"WalletLogin_{Unity.Netcode.NetworkManager.Singleton.LocalClientId}_{System.Guid.NewGuid()}";
        gameObject.name = uniqueName;
        OnUnityReady();
        RegisterWalletLoginObject(gameObject.name);
        if (metaMaskLoginButton != null)
        {
            metaMaskLoginButton.gameObject.SetActive(true);
            metaMaskLoginButton.onClick.AddListener(OnMetaMaskLoginClicked);
        }
#else
        // This logic is for WebGL clients only.
        if (metaMaskLoginButton != null)
        {
            metaMaskLoginButton.gameObject.SetActive(false);
        }
#endif
    }

    // Disables wallet login UI for non-owners.
    public void DisableForNonOwner()
    {
        if (metaMaskLoginButton != null)
            metaMaskLoginButton.gameObject.SetActive(false);
    }

    // Called from JS (WebGL): sets address, updates UI, fetches balance.
    public void OnWalletConnected(string address)
    {
        SetNetworkedAddress(address);
        UpdatePlayerUI(address, null);
        FetchBalance(address);
    }

    public void OnWalletConnectionFailed(string error)
    {
        UpdatePlayerUI(null, $"Wallet connection failed: {error}");
    }

    public void OnBalanceReceived(string hexValue)
    {
        string balanceText;
        try
        {
            var value = System.Numerics.BigInteger.Parse(hexValue.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
            var balanceEth = Nethereum.Util.UnitConversion.Convert.FromWei(value);
            balanceText = $"{balanceEth:F4} ETH";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse balance: {hexValue}. Error: {e.Message}");
            balanceText = "Failed to parse balance.";
        }
        UpdatePlayerUI(null, balanceText);
    }

    public void OnBalanceError(string error)
    {
        UpdatePlayerUI(null, $"Balance error: {error}");
    }

    public void OnMetaMaskLoginClicked()
    {
        MetaMaskInterop.Connect();
    }

    private void FetchBalance(string address)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FetchBalanceJS(address);
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FetchBalanceJS(string address);
    [DllImport("__Internal")]
    private static extern void RegisterWalletLoginObject(string objectName);
    [DllImport("__Internal")]
    private static extern void OnUnityReady();
#endif

    private void SetNetworkedAddress(string address)
    {
        _walletAddress = address; // Store the address for external access
        if (_playerController != null && _playerController.IsOwner)
            _playerController.SetAddressServerRpc(address);
    }

    private void UpdatePlayerUI(string address, string balance)
    {
        if (_playerController != null)
        {
            if (address != null) _playerController.SetAddressHUD(address);
            if (balance != null) _playerController.SetBalanceHUD(balance);
        }
    }

    // Finds the local player's UserUser instance for callback routing.
    private static UserUser GetLocalPlayerUserUser()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsOwner)
                return player.GetComponentInChildren<UserUser>(true);
        }
        return null;
    }

    // --- MetaMask/JS Callbacks ---
    public void OnWalletConnectedStatic(string address) => OnWalletConnected(address);
    public void OnWalletConnectionFailedStatic(string error) => OnWalletConnectionFailed(error);
    public void OnBalanceReceivedStatic(string hexValue) => OnBalanceReceived(hexValue);
    public void OnBalanceErrorStatic(string error) => OnBalanceError(error);

    public void OnSignTransferSubmittedStatic(string txHash)
    {
        Debug.Log($"[WalletLogin] SignTransfer submitted: {txHash}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
            userUserInstance.OnSignTransferSubmitted(txHash);
        else
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnSignTransferSubmittedStatic.");
    }

    public void OnSignTransferConfirmedStatic(string txHash)
    {
        Debug.Log($"[WalletLogin] SignTransfer confirmed: {txHash}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
            userUserInstance.OnSignTransferConfirmed(txHash);
        else
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnSignTransferConfirmedStatic.");
    }

    public void OnSignTransferErrorStatic(string error)
    {
        Debug.LogWarning($"[WalletLogin] SignTransfer error: {error}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
            userUserInstance.OnSignTransferError(error);
        else
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnSignTransferErrorStatic.");
    }

    public void OnSignedMessageReceivedStatic(string message)
    {
        Debug.Log($"[WalletLogin] Signed message received: {message}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
            userUserInstance.OnSignedMessageReceived(message);
        else
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnSignedMessageReceivedStatic.");
    }

    public void OnSignedMessageErrorStatic(string error)
    {
        Debug.LogWarning($"[WalletLogin] Signed message error: {error}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
            userUserInstance.OnSignedMessageError(error);
        else
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnSignedMessageErrorStatic.");
    }

    public void OnInitiateTransferSubmittedStatic(string txHash)
    {
        Debug.Log($"[WalletLogin] InitiateTransfer submitted: {txHash}");
        var userUserInstance = GetLocalPlayerUserUser();
        if (userUserInstance != null)
        {
            userUserInstance.OnInitiateTransferSubmitted(txHash);
            // Notify recipient only after MetaMask confirms transaction submission
            userUserInstance.NotifyRecipientAfterTransfer();
        }
        else
        {
            Debug.LogWarning("[WalletLogin] UserUser instance not found for OnInitiateTransferSubmittedStatic.");
        }
    }
}