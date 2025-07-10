using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using TMPro;

// Server-side device manager that handles both temperature sensor and fan controller
// Communicates with DeviceDevice smart contract using function selectors
// Clients receive updates via NetworkVariables for visual sync

public class DeviceManager : NetworkBehaviour
{
    [Header("Device Objects")]
    [SerializeField] private Transform fanBlade;
    [SerializeField] private TextMeshPro temperatureDisplay;
    
    [Header("Blockchain Configuration")]
    [SerializeField] private string rpcUrl = "http://localhost:8545";
    [SerializeField] private string contractAddress = ""; // Set in inspector
    [SerializeField] private string sensorPrivateKey = ""; // Set in inspector
    
    // Function selectors for DeviceDevice contract
    private const string UPDATE_TEMPERATURE_SELECTOR = "0x076e48a1"; // updateTemperature(int256)
    private const string GET_TEMPERATURE_SELECTOR = "0x6421d04b";    // getTemperature()
    
    // Network synchronized values for clients
    public NetworkVariable<int> syncedTemperature = new NetworkVariable<int>(
        25, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public NetworkVariable<float> syncedFanSpeed = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Server-side blockchain components
    private Web3 web3;
    private Account sensorAccount;
    private float currentRotationSpeed = 100f;
    
    public override void OnNetworkSpawn()
    {
        // Subscribe to value changes for client-side visual updates
        syncedTemperature.OnValueChanged += OnTemperatureChanged;
        syncedFanSpeed.OnValueChanged += OnFanSpeedChanged;
        
        if (IsServer)
        {
            InitializeBlockchainConnection();
            
            // Runs the device cycle every 10 seconds, starting 2 seconds after spawn
            InvokeRepeating(nameof(DeviceCycle), 2f, 10f);
        }
        
        // Initialize display for current values
        UpdateTemperatureDisplay(syncedTemperature.Value);
        UpdateFanRotation(syncedFanSpeed.Value);
    }
    
    private void InitializeBlockchainConnection()
    {
        if (string.IsNullOrEmpty(contractAddress) || string.IsNullOrEmpty(sensorPrivateKey))
        {
            Debug.LogError("[DeviceManager] Contract address or sensor private key not configured!");
            return;
        }
        
        try
        {
            sensorAccount = new Account(sensorPrivateKey);
            web3 = new Web3(sensorAccount, rpcUrl);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DeviceManager] Failed to initialize blockchain: {e.Message}");
        }
    }
    
    // Main device cycle: simulate temperature, upload to blockchain, read back, and control fan
    private async void DeviceCycle()
    {
        if (!IsServer || web3 == null) return;
        
        // 1. Generate simulated temperature (20-30°C)
        int newTemperature = Random.Range(20, 31);
        
        // 2. Upload temperature to blockchain
        await UploadTemperatureToContract(newTemperature);
        
        // 3. Read temperature from contract (simulating fan controller reading)
        int contractTemperature = await ReadTemperatureFromContract();
        
        // 4. Update fan speed based on contract temperature
        UpdateFanSpeedFromTemperature(contractTemperature);
        
        // 5. Sync values to clients
        syncedTemperature.Value = contractTemperature;
    }
    
    private async Task UploadTemperatureToContract(int temperature)
    {
        try
        {
            // Encode function call using selector + padded parameters
            string temperatureHex = ((uint)temperature).ToString("x").PadLeft(64, '0');
            string callData = UPDATE_TEMPERATURE_SELECTOR + temperatureHex;
            
            var transactionInput = new Nethereum.RPC.Eth.DTOs.TransactionInput()
            {
                To = contractAddress,
                From = sensorAccount.Address,
                Data = callData,
                Gas = new HexBigInteger(300000),
                GasPrice = new HexBigInteger(0) // For local/test networks
            };
            
            await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DeviceManager] Failed to upload temperature: {e.Message}");
        }
    }
    
    private async Task<int> ReadTemperatureFromContract()
    {
        try
        {
            // Call getTemperature() using function selector
            var callInput = new Nethereum.RPC.Eth.DTOs.CallInput()
            {
                To = contractAddress,
                Data = GET_TEMPERATURE_SELECTOR
            };
            
            var result = await web3.Eth.Transactions.Call.SendRequestAsync(callInput);
            
            // Parse result (int256 returned as hex)
            if (!string.IsNullOrEmpty(result) && result.Length >= 2)
            {
                string hexValue = result.Substring(2); // Remove 0x prefix
                if (hexValue.Length >= 64)
                {
                    // Take last 64 characters for int256
                    string temperatureHex = hexValue.Substring(hexValue.Length - 64);
                    
                    // Convert hex to int (handling negative values for int256)
                    var bigInt = System.Numerics.BigInteger.Parse("0" + temperatureHex, System.Globalization.NumberStyles.HexNumber);
                    int temperature = (int)bigInt;

                    return temperature;
                }
            }
            
            Debug.LogWarning("[DeviceManager] Invalid response from contract");
            return 25; // Default fallback
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DeviceManager] Failed to read temperature: {e.Message}");
            return 25; // Default fallback
        }
    }
    
    private void UpdateFanSpeedFromTemperature(int temperature)
    {
        // Calculate fan speed: 100 RPM at 20°C, 1000 RPM at 30°C
        float normalizedTemp = Mathf.Clamp01((temperature - 20f) / 10f);
        float newFanSpeed = Mathf.Lerp(100f, 1000f, normalizedTemp);       
        syncedFanSpeed.Value = newFanSpeed;
    }
    
    private void OnTemperatureChanged(int previousValue, int newValue)
    {
        UpdateTemperatureDisplay(newValue);
    }
    
    private void OnFanSpeedChanged(float previousValue, float newValue)
    {
        UpdateFanRotation(newValue);
    }
    
    private void UpdateTemperatureDisplay(int temperature)
    {
        if (temperatureDisplay != null)
        {
            temperatureDisplay.text = $"Temp: {temperature}°C";
        }
    }
    
    private void UpdateFanRotation(float speed)
    {
        currentRotationSpeed = speed;
    }
    
    void Update()
    {
        // Animate fan blade rotation
        if (fanBlade != null)
        {
            fanBlade.Rotate(Vector3.left * currentRotationSpeed * Time.deltaTime);
        }
    }
    
    public override void OnDestroy()
    {
        syncedTemperature.OnValueChanged -= OnTemperatureChanged;
        syncedFanSpeed.OnValueChanged -= OnFanSpeedChanged;
    }
}
