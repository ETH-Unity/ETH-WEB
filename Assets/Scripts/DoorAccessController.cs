using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public enum DoorType
{
    Digital,
    Physical,
    AdminRoom
}

public class DoorAccessController : NetworkBehaviour
{
    [Header("Contract Settings")]
    [SerializeField] private string contractAddress;
    [SerializeField] private DoorType doorType = DoorType.Digital;
    [SerializeField] private GameObject doorGameObject;

    [Header("Animation Settings")]
    [SerializeField] private Vector3 openOffset = new Vector3(1.5f, 0, 0);
    [SerializeField] private float moveSpeed = 1f;

    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private bool isPlayerNearby = false;
    private bool shouldMove = false;
    private Collider playerCollider;

    private NetworkVariable<bool> isDoorOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        // Set contract address from config if not set in inspector
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }
        if (doorGameObject != null)
        {
            closedPosition = doorGameObject.transform.position;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (shouldMove && doorGameObject != null)
        {
            doorGameObject.transform.position = Vector3.MoveTowards(
                doorGameObject.transform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );

            if (Vector3.Distance(doorGameObject.transform.position, targetPosition) < 0.01f)
            {
                shouldMove = false;
                Debug.Log(isDoorOpen.Value ? "✅ Door fully opened." : "✅ Door fully closed.");
            }
        }
    }

    private void LateUpdate()
    {
        if (!isPlayerNearby) return;
        if (EventSystem.current.currentSelectedGameObject != null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isDoorOpen.Value)
            {
                Debug.Log($"🔍 Checking access for {doorType} before opening...");
                CheckDoorAccess();
            }
            else
            {
                Debug.Log($"🔒 Closing {doorType} door...");
                CloseDoorServerRpc();
            }
        }
    }

    public void CheckDoorAccess()
    {
        // Always fetch contract address from config before call
        if (ClientConfigLoader.Config != null && !string.IsNullOrWhiteSpace(ClientConfigLoader.Config.UserDeviceContractAddress))
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }

        if (playerCollider == null)
        {
            Debug.LogError("Player collider not found. Cannot check access.");
            return;
        }

        WalletLogin walletLogin = playerCollider.GetComponentInChildren<WalletLogin>(true);

        if (walletLogin == null || string.IsNullOrEmpty(walletLogin.WalletAddress))
        {
            Debug.LogError("❌ Wallet address is missing – connect MetaMask first.");
            return;
        }

        string walletAddress = walletLogin.WalletAddress.Trim().ToLower();
        if (!walletAddress.StartsWith("0x") || walletAddress.Length != 42)
        {
            Debug.LogError("❌ Invalid Ethereum address: " + walletAddress);
            return;
        }

        string functionSelector = GetFunctionSelector(doorType);
        string paddedAddress = walletAddress.Substring(2).PadLeft(64, '0');
        string callData = functionSelector + paddedAddress;

        DoorAccessBridge.CurrentTarget = this;

        Debug.Log($"📡 Sending contract call: {callData}");
        MetaMaskInterop.Call(contractAddress, callData);
    }

    private string GetFunctionSelector(DoorType type)
    {
        return type switch
        {
            DoorType.Digital => "0xb7d52701",
            DoorType.Physical => "0x939cc763",
            DoorType.AdminRoom => "0x3922d748",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public void OnDoorAccessResult(string result)
    {
        Debug.Log($"📬 Contract result for {doorType}: {result}");

        string trueValue = "0x" + new string('0', 63) + "1";

        if (result == "0x1" || result.ToLower() == trueValue.ToLower())
        {
            Debug.Log($"✅ Access granted to {doorType} door.");

            if (isPlayerNearby && !isDoorOpen.Value)
                OpenDoorServerRpc();
        }
        else
        {
            Debug.Log($"❌ Access denied to {doorType} door.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenDoorServerRpc()
    {
        if (doorGameObject == null) return;

        targetPosition = closedPosition + doorGameObject.transform.TransformDirection(openOffset);
        shouldMove = true;
        isDoorOpen.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CloseDoorServerRpc()
    {
        if (doorGameObject == null) return;

        targetPosition = closedPosition;
        shouldMove = true;
        isDoorOpen.Value = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null || !networkObject.IsOwner) return;

        Debug.Log("🚶 Player entered near the door");
        isPlayerNearby = true;
        playerCollider = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == playerCollider)
        {
            Debug.Log("🚪 Player left the area near the door");
            isPlayerNearby = false;
            playerCollider = null;
        }
    }

    public void OnWalletConnectionFailed(string error)
    {
        Debug.LogError($"❌ MetaMask error: {error}");
    }
}
