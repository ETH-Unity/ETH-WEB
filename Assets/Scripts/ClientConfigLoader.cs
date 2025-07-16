using Unity.Netcode;
using UnityEngine;
using System;

[Serializable]
public class ClientConfig
{
    public string UserUserContractAddress;
    public string UserDeviceContractAddress;
    public string DocumentHashhingContractAddress;
    public string NFTContractAddress;
}

public class ClientConfigLoader : NetworkBehaviour
{
    public static ClientConfig Config { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsClient && IsOwner)
        {
            // Always request config from server on spawn
            RequestConfigServerRpc();
        }
    }

    [ServerRpc]
    private void RequestConfigServerRpc(ServerRpcParams rpcParams = default)
    {
        var networkStartup = FindObjectOfType<NetworkStartup>();
        if (networkStartup != null && networkStartup.config != null)
        {
            // Validate contract addresses before sending
            string userUser = ValidateAddress(networkStartup.config.UserUserContractAddress);
            string userDevice = ValidateAddress(networkStartup.config.UserDeviceContractAddress);
            string documentSigning = ValidateAddress(networkStartup.config.DocumentHashhingContractAddress);
            string nft = ValidateAddress(networkStartup.config.NFTContractAddress);

            SendConfigClientRpc(
                userUser,
                userDevice,
                documentSigning,
                nft,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } }
            );
        }
    }

    [ClientRpc]
    private void SendConfigClientRpc(string userUser, string userDevice, string documentSigning, string nft, ClientRpcParams clientRpcParams = default)
    {
        Config = new ClientConfig
        {
            UserUserContractAddress = ValidateAddress(userUser),
            UserDeviceContractAddress = ValidateAddress(userDevice),
            DocumentHashhingContractAddress = ValidateAddress(documentSigning),
            NFTContractAddress = ValidateAddress(nft)
        };
        Debug.Log($"ClientConfigLoader: Config received from server.\nUserUser: {Config.UserUserContractAddress}\nUserDevice: {Config.UserDeviceContractAddress}\nDocumentHash: {Config.DocumentHashhingContractAddress}\nNFT: {Config.NFTContractAddress}");
        // Notify other components if needed
    }

    private string ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || !address.StartsWith("0x") || address.Length != 42)
        {
            Debug.LogWarning($"ClientConfigLoader: Invalid contract address received: '{address}'");
            return string.Empty;
        }
        return address;
    }
}
