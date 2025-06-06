using UnityEngine;
using System.Collections;
using Nethereum.Signer;
using Nethereum.Unity.Rpc;
using Nethereum.RPC.Eth.DTOs;
using TMPro;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Numerics;
using System;

// WalletLogin.cs
// Handles per-player wallet login (MetaMask for WebGL, private key for Editor),
// wallet address network sync, and balance display

public class WalletLogin : MonoBehaviour
{
    [SerializeField] private Button metaMaskLoginButton;
    [SerializeField] private TMP_InputField privateKeyInputField;
    private string rpcUrl = "http://localhost:8545/";
    private PlayerController _playerController;

    void Awake()
    {
        // Get the PlayerController on the parent (player prefab)
        _playerController = GetComponentInParent<PlayerController>();
    }

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // --- WebGL: Setup unique JS interop and UI ---
        // Give WalletLogin a unique name per player and register for JS callbacks
        string uniqueName = $"WalletLogin_{Unity.Netcode.NetworkManager.Singleton.LocalClientId}_{System.Guid.NewGuid()}";
        gameObject.name = uniqueName;
        OnUnityReady();
        RegisterWalletLoginObject(gameObject.name);
        // Remove private key input field for WebGL
        if (privateKeyInputField != null)
            Destroy(privateKeyInputField.gameObject);
        // Always enable MetaMask button for the local player
        if (metaMaskLoginButton != null)
            metaMaskLoginButton.gameObject.SetActive(true);
        if (metaMaskLoginButton != null)
            metaMaskLoginButton.onClick.AddListener(OnMetaMaskLoginClicked);
#else
        // --- Editor/Standalone: Setup UI and input ---
        // Hide login UI for non-owners
        if (_playerController != null && !_playerController.IsOwner)
        {
            if (metaMaskLoginButton != null)
                metaMaskLoginButton.gameObject.SetActive(false);
            if (privateKeyInputField != null)
                privateKeyInputField.gameObject.SetActive(false);
            return;
        }
        if (metaMaskLoginButton != null)
            metaMaskLoginButton.onClick.AddListener(OnMetaMaskLoginClicked);
        if (privateKeyInputField != null)
            privateKeyInputField.onSubmit.AddListener(delegate { OnLoginClicked(); });
#endif
    }

    // Called when the user submits their private key (Editor/Standalone)
    public void OnLoginClicked()
    {
        if (_playerController != null && !_playerController.IsOwner)
            return;
        if (privateKeyInputField == null)
            return;
        string privateKey = privateKeyInputField.text;
        if (string.IsNullOrEmpty(privateKey))
            return;
        StartCoroutine(LoginRoutine(privateKey));
    }

    // Coroutine: Handles private key login and balance fetch (Editor/Standalone)
    private IEnumerator LoginRoutine(string privateKey)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        yield break;
#else
        string address = null;
        try
        {
            var key = new Nethereum.Signer.EthECKey(privateKey);
            address = key.GetPublicAddress();
        }
        catch (Exception)
        {
            yield break;
        }
        SetNetworkedAddress(address);
        UpdatePlayerUI(address, null);
        FetchBalance(address);
        yield break;
#endif
    }

    // Called from JS (WebGL) or after login (Editor): sets address, updates UI, fetches balance
    public void OnWalletConnected(string address)
    {
        SetNetworkedAddress(address);
        UpdatePlayerUI(address, null);
        FetchBalance(address);
    }

    // Called from JS (WebGL) on connection failure
    public void OnWalletConnectionFailed(string error)
    {
        UpdatePlayerUI(null, $"Wallet connection failed: {error}");
    }

    // Called from JS (WebGL) or after balance fetch (Editor): updates balance in UI
    public void OnBalanceReceived(string hexValue)
    {
        string balanceText;
        try
        {
            var value = System.Numerics.BigInteger.Parse(hexValue.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
            var balanceEth = Nethereum.Util.UnitConversion.Convert.FromWei(value);
            balanceText = $"Balance: {balanceEth} ETH";
        }
        catch (Exception)
        {
            balanceText = "Failed to parse balance.";
        }
        UpdatePlayerUI(null, balanceText);
    }

    // Called from JS (WebGL) or after balance fetch error (Editor)
    public void OnBalanceError(string error)
    {
        UpdatePlayerUI(null, $"Balance error: {error}");
    }

    // Called when the MetaMask login button is clicked (WebGL)
    public void OnMetaMaskLoginClicked()
    {
        if (_playerController != null && !_playerController.IsOwner)
            return;
        MetaMaskInterop.Connect();
    }

    // Fetches the balance for the given address (WebGL or Editor)
    private void FetchBalance(string address)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        FetchBalanceJS(address);
        #else
        StartCoroutine(FetchBalanceEditor(address));
        #endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    // Coroutine: Fetches balance using Nethereum (Editor/Standalone)
    private IEnumerator FetchBalanceEditor(string address)
    {
        var request = new EthGetBalanceUnityRequest(rpcUrl);
        yield return request.SendRequest(address, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());
        if (request.Exception == null)
        {
            var value = request.Result.Value;
            var balanceEth = Nethereum.Util.UnitConversion.Convert.FromWei(value);
            string balanceText = $"Balance: {balanceEth} ETH";
            UpdatePlayerUI(null, balanceText);
        }
        else
        {
            UpdatePlayerUI(null, $"Balance error: {request.Exception.Message}");
        }
    }
#endif

    // JS interop for WebGL
    [DllImport("__Internal")]
    private static extern void FetchBalanceJS(string address);
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterWalletLoginObject(string objectName);
    [DllImport("__Internal")]
    private static extern void OnUnityReady();
#endif

    // Sets the networked wallet address for the local player (calls ServerRpc)
    private void SetNetworkedAddress(string address)
    {
        if (_playerController != null && _playerController.IsOwner)
        {
            _playerController.SetAddressServerRpc(address);
        }
    }

    // Updates the local player's HUD and balance UI
    private void UpdatePlayerUI(string address, string balance)
    {
        if (_playerController != null)
        {
            if (address != null) _playerController.SetAddressHUD(address);
            if (balance != null) _playerController.SetBalanceHUD(balance);
        }
    }

    // Static wrappers for JS SendMessage callbacks (WebGL)
    public void OnWalletConnectedStatic(string address) => OnWalletConnected(address);
    public void OnWalletConnectionFailedStatic(string error) => OnWalletConnectionFailed(error);
    public void OnBalanceReceivedStatic(string hexValue) => OnBalanceReceived(hexValue);
    public void OnBalanceErrorStatic(string error) => OnBalanceError(error);
}