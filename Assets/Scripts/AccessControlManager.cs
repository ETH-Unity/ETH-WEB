using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;

public class AccessControlManager : NetworkBehaviour
{
    [Header("Contract Settings")]
    [SerializeField] private string contractAddress;

    // UI Elements - Connect these in the Unity Editor
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField userAddressInput;
    [SerializeField] private TMP_Dropdown roleDropdown;
    [SerializeField] private Toggle physicalAccessToggle;
    [SerializeField] private Toggle digitalAccessToggle;
    [SerializeField] private Toggle adminRoomAccessToggle;
    [SerializeField] private TMP_InputField expirationInput;
    [SerializeField] private PlayerListUI playerListUI;
    [SerializeField] private TMP_Text selectedAddressText;

    [Header("Buttons")]
    [SerializeField] private Button grantAccessButton;
    [SerializeField] private Button changeAccessButton;
    [SerializeField] private Button revokeAccessButton;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            this.enabled = false;
            return;
        }

        // Assign listeners to the buttons
        if (grantAccessButton != null) grantAccessButton.onClick.AddListener(GrantAccess);
        if (changeAccessButton != null) changeAccessButton.onClick.AddListener(ChangeAccess);
        if (revokeAccessButton != null) revokeAccessButton.onClick.AddListener(RevokeAccess);

        // Listen for address selection from the player list
        if (playerListUI != null)
        {
            playerListUI.OnAddressSelected += OnPlayerAddressSelected;
        }

        // Listen for manual changes to the input field
        if (userAddressInput != null)
        {
            userAddressInput.onValueChanged.AddListener(OnInputAddressChanged);
        }

        // Set initial status text
        UpdateSelectedAddressText(userAddressInput.text);
    }

    private void OnPlayerAddressSelected(string address)
    {
        if (userAddressInput != null)
        {
            userAddressInput.text = address;
        }
        UpdateSelectedAddressText(address);
    }

    private void OnInputAddressChanged(string address)
    {
        UpdateSelectedAddressText(address);
    }

    private void UpdateSelectedAddressText(string address)
    {
        if (selectedAddressText == null) return;

        if (string.IsNullOrWhiteSpace(address))
        {
            selectedAddressText.text = "No address selected. Please type one or select from the list.";
        }
        else
        {
            string displayText = address;
            if (address.Length > 8)
            {
                displayText = $"{address.Substring(0, 5)}...{address.Substring(address.Length - 3)}";
            }
            selectedAddressText.text = $"Modifying access for: {displayText}";
        }
    }

    // Function selectors for the smart contract
    private const string GRANT_ACCESS_SELECTOR = "0x2cfe3b02";
    private const string REVOKE_ACCESS_SELECTOR = "0x85e68531";
    private const string CHANGE_ACCESS_SELECTOR = "0xd53d720c";

    private string GetRoleValue()
    {
        // Role definitions in contract: 1=Default, 2=Service, 3=Admin
        return (roleDropdown.value + 1).ToString();
    }

    private void SendAccessTransaction(string functionSelector)
    {
        // Ensure contract address is set from config if empty
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }
        string userAddress = userAddressInput.text;
        string expirationString = expirationInput.text;

        if (!ValidateInputs(userAddress, expirationString)) return;

        string role = GetRoleValue();
        bool physicalAccess = physicalAccessToggle.isOn;
        bool digitalAccess = digitalAccessToggle.isOn;
        bool adminRoomAccess = adminRoomAccessToggle.isOn;

        uint.TryParse(expirationString, out uint expirationMinutes);
        uint expirationTimestamp;
        if (expirationMinutes == 0)
        {
            expirationTimestamp = 0; // Represents no expiration
        }
        else
        {
            // Calculate the Unix timestamp for the expiration
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            expirationTimestamp = (uint)(currentTimestamp + (long)expirationMinutes * 60);
        }

        string data = functionSelector +
                      userAddress.Substring(2).PadLeft(64, '0') +
                      uint.Parse(role).ToString("x").PadLeft(64, '0') +
                      (physicalAccess ? "1" : "0").PadLeft(64, '0') +
                      (digitalAccess ? "1" : "0").PadLeft(64, '0') +
                      (adminRoomAccess ? "1" : "0").PadLeft(64, '0') +
                      expirationTimestamp.ToString("x").PadLeft(64, '0');

        Debug.Log($"Calling contract with data: {data}");
        MetaMaskInterop.SendTx(contractAddress, data, "0x0", gameObject.name, "OnTransactionSuccess", "OnTransactionError");
    }

    public void GrantAccess()
    {
        SendAccessTransaction(GRANT_ACCESS_SELECTOR);
    }

    public void ChangeAccess()
    {
        // Always fetch contract address from config before call
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }
        SendAccessTransaction(CHANGE_ACCESS_SELECTOR);
    }

    public void RevokeAccess()
    {
        // Always fetch contract address from config before call
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }
        string userAddress = userAddressInput.text;

        if (!ValidateAddress(userAddress)) return;

        string data = REVOKE_ACCESS_SELECTOR + userAddress.Substring(2).PadLeft(64, '0');

        Debug.Log("Calling RevokeAccess with data: " + data);
        MetaMaskInterop.SendTx(contractAddress, data, "0x0", gameObject.name, "OnTransactionSuccess", "OnTransactionError");
    }

    private bool ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || !address.StartsWith("0x") || address.Length != 42)
        {
            Debug.LogError("Invalid address provided. Please check the address.");
            return false;
        }
        return true;
    }

    private bool ValidateInputs(string address, string expiration)
    {
        if (!ValidateAddress(address)) return false;

        if (!string.IsNullOrEmpty(expiration) && !uint.TryParse(expiration, out _))
        {
            Debug.LogError("Invalid expiration value. Please provide a valid number for minutes (or leave empty for no expiration).");
            return false;
        }
        return true;
    }

    public void OnTransactionSuccess(string txHash)
    {
        Debug.Log($"Transaction successful with hash: {txHash}");
    }

    public void OnTransactionError(string errorMessage)
    {
        Debug.LogError($"Transaction failed: {errorMessage}");
    }
}
