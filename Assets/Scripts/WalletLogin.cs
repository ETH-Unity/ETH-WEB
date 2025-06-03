using UnityEngine;
using System.Collections;
using Nethereum.Signer;
using Nethereum.Unity.Rpc;
using Nethereum.RPC.Eth.DTOs;
using TMPro;

public class WalletLogin : MonoBehaviour
{
    public TMP_InputField privateKeyInput;
    public TMP_Text addressText, balanceText, username;
    private string rpcUrl = "http://localhost:8545/";

    void Start()
    {
        privateKeyInput.onEndEdit.AddListener(OnPrivateKeyEndEdit);
    }

    private void OnPrivateKeyEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || !string.IsNullOrEmpty(input))
        {
            OnLoginClicked();
        }
    }

    public void OnLoginClicked()
    {
        string privateKey = privateKeyInput.text.Trim();
        if (privateKey.StartsWith("0x") || privateKey.StartsWith("0X"))
        {
            privateKey = privateKey.Substring(2);
        }
        if (string.IsNullOrEmpty(privateKey) || privateKey.Length != 64)
        {
            addressText.text = "Invalid private key!";
            balanceText.text = "";
            Debug.LogWarning("[WalletLogin] Invalid private key entered.");
            return;
        }
        StartCoroutine(LoginRoutine(privateKey));
    }

    private IEnumerator LoginRoutine(string privateKey)
    {
        // Derive address
        var key = new EthECKey(privateKey);
        string address = key.GetPublicAddress();
        addressText.text = $"Address: {address}";
        SetNetworkedAddress(address);

        // Fetch balance
        var rpcFactory = new UnityWebRequestRpcClientFactory(rpcUrl);
        var balanceRequest = new EthGetBalanceUnityRequest(rpcFactory);

        yield return balanceRequest.SendRequest(address, BlockParameter.CreateLatest());

        if (balanceRequest.Exception != null)
        {
            Debug.LogError($"[WalletLogin] Balance fetch error: {balanceRequest.Exception.Message}");
        }
        else
        {
            var balanceWei = balanceRequest.Result.Value;
            var balanceEth = Nethereum.Util.UnitConversion.Convert.FromWei(balanceWei);
            balanceText.text = $"Balance: {balanceEth} ETH";
        }
    }
    private void SetNetworkedAddress(string address)
    {
        // Find the local player object and set the address via ServerRpc
        foreach (var netObj in Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            var player = netObj.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                player.SetAddressServerRpc(address);
                break;
            }
        }
    }

}