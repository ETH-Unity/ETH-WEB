using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkStartup : MonoBehaviour
{
    public string serverIpAddress = "127.0.0.1";
    public ushort serverPort = 7777;

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkStartup: NetworkManager.Singleton is NULL. Ensure a NetworkManager GameObject with NetworkManager component exists in the scene.");
            enabled = false;
            return;
        }
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("NetworkStartup: UnityTransport component not found on the NetworkManager GameObject. Please add it.");
            enabled = false;
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        if (Application.isEditor)
        {
            transport.SetConnectionData("0.0.0.0", serverPort);
            transport.UseWebSockets = true;
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            transport.UseWebSockets = true;
            transport.SetConnectionData(serverIpAddress, serverPort);
            NetworkManager.Singleton.StartClient();
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("NetworkStartup: Server is up and running.");
    }

    private void OnTransportFailure()
    {
        Debug.LogError("NetworkStartup: Transport failure (e.g., port in use, configuration issue).");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"NetworkStartup: Client connected - ClientId: {clientId}");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"NetworkStartup: Client disconnected - ClientId: {clientId}");
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }
    }
}