using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using TMPro;

public class NetworkStartup : MonoBehaviour
{
    [Serializable]
    public class ServerConfig
    {
        public string UserUserContractAddress;
        public string UserDeviceContractAddress;
        public string DeviceDeviceContractAddress;
        public string DeviceDeviceWalletPrivateKey;
        public string DocumentHashhingContractAddress;
        public string NFTContractAddress;
        public string rpcUrl;
        public string chainId;
        // Optionally, keep serverIpAddress and serverPort for networking
        public string serverIpAddress = "127.0.0.1";
        public ushort serverPort = 7777;
    }

    public ServerConfig config = new ServerConfig();
    private string configFileName = "config.json";

    [Header("Status UI")]
    public TMP_Text statusText; // Assign in Inspector

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: do not load config file
        if (NetworkManager.Singleton == null)
        {
            LogStatus("NetworkManager.Singleton is NULL. Ensure a NetworkManager GameObject with NetworkManager component exists in the scene.");
            enabled = false;
            return;
        }
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            LogStatus("UnityTransport component not found on the NetworkManager GameObject. Please add it.");
            enabled = false;
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        transport.UseWebSockets = true;
        transport.SetConnectionData(config.serverIpAddress, config.serverPort);
        LogStatus($"Starting client (WebGL). Connecting to {config.serverIpAddress}:{config.serverPort}");
        NetworkManager.Singleton.StartClient();
#else
        // Server: load config file
        LoadConfig();
        if (NetworkManager.Singleton == null)
        {
            LogStatus("NetworkManager.Singleton is NULL. Ensure a NetworkManager GameObject with NetworkManager component exists in the scene.");
            enabled = false;
            return;
        }
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            LogStatus("UnityTransport component not found on the NetworkManager GameObject. Please add it.");
            enabled = false;
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        transport.SetConnectionData("0.0.0.0", config.serverPort);
        transport.UseWebSockets = true;
        LogStatus("Starting server (non-WebGL)...");
        NetworkManager.Singleton.StartServer();
#endif
    }

    private void LoadConfig()
    {
        // Always load from the folder where the .exe is located
        string exeDir = System.AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.Combine(exeDir, configFileName);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                config = JsonUtility.FromJson<ServerConfig>(json);
                LogStatus($"Loaded config from {path}");
                // Print all contract addresses
                LogStatus($"UserUserContractAddress: {config.UserUserContractAddress}");
                LogStatus($"UserDeviceContractAddress: {config.UserDeviceContractAddress}");
                LogStatus($"DeviceDeviceContractAddress: {config.DeviceDeviceContractAddress}");
                LogStatus($"DocumentHashhingContractAddress: {config.DocumentHashhingContractAddress}");
                LogStatus($"NFTContractAddress: {config.NFTContractAddress}");
                LogStatus($"rpcUrl: {config.rpcUrl}");
                LogStatus($"chainId: {config.chainId}");
            }
            catch (Exception e)
            {
                LogStatus($"Failed to load config file: {e.Message}");
            }
        }
        else
        {
            LogStatus($"Config file not found at {path}, using defaults.");
        }
    }

    private void OnServerStarted()
    {
        LogStatus("Server is up and running.");
    }

    private void OnTransportFailure()
    {
        LogStatus("Transport failure (e.g., port in use, configuration issue).");
    }

    private void OnClientConnected(ulong clientId)
    {
        LogStatus($"Client connected - ClientId: {clientId}");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        LogStatus($"Client disconnected - ClientId: {clientId}");
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

    private void LogStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text += message + "\n";
        }
        Debug.Log(message);
    }
}