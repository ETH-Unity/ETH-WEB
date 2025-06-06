using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class WalletDebug : MonoBehaviour
{
    public KeyCode printKey = KeyCode.F9;

    void Update()
    {
        if (Input.GetKeyDown(printKey))
        {
            PrintAllWalletAddresses();
        }
    }

    private void PrintAllWalletAddresses()
    {
        // Only print on the server/host
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[WalletDebugPrinter] Only the server/host can print all wallet addresses.");
            return;
        }
        var players = FindObjectsOfType<PlayerController>();
        Debug.Log($"[WalletDebugPrinter] Listing all player wallet addresses:");
        foreach (var player in players)
        {
            Debug.Log($"ClientId: {player.OwnerClientId} | Address: {player.walletAddress.Value}");
        }
    }
}
