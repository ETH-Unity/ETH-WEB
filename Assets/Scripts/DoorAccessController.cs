using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class DoorAccessController : MonoBehaviour
{
    [Header("Contract Settings")]
    [SerializeField] private string contractAddress;
    [SerializeField] private GameObject doorGameObject;

    [Header("Animation Settings")]
    [SerializeField] private Vector3 openOffset = new Vector3(1.5f, 0, 0);
    [SerializeField] private float moveSpeed = 1f;

    private bool shouldMove = false;
    private bool hasAccess = false;
    private bool doorOpened = false;
    private bool isPlayerNearby = false;
    private Vector3 targetPosition;
    private Vector3 closedPosition;
    private WalletLogin walletLogin;
    
    private void Start()
    {
        if (doorGameObject != null)
        {
            closedPosition = doorGameObject.transform.position;
        }
    }

    public void CheckDoorAccess()
    {
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

        string functionSelector = "0xb7d52701";
        string paddedAddress = walletAddress.Substring(2).PadLeft(64, '0');
        string callData = functionSelector + paddedAddress;

        Debug.Log($"📡 Sending call: {callData}");
        MetaMaskInterop.Call(contractAddress, callData);
    }

    public void OnDoorAccessResult(string result)
    {
        Debug.Log($"📬 Smart contract responded: {result}");

        string trueValue = "0x" + new string('0', 63) + "1";

        if (result == "0x1" || result.ToLower() == trueValue.ToLower())
        {
            Debug.Log("✅ User has access to open the door.");
            hasAccess = true;

            if (isPlayerNearby && !doorOpened)
            {
                Debug.Log("🔓 Automatically opening the door after access granted.");
                OpenDoor();
            }
        }
        else
        {
            Debug.Log("❌ User does NOT have access.");
        }
    }

    private void OpenDoor()
    {
        if (doorGameObject != null)
        {
            Debug.Log("▶️ Starting door opening movement (position).");
            targetPosition = doorGameObject.transform.position + openOffset;
            shouldMove = true;
            doorOpened = true;
        }
        else
        {
            Debug.LogWarning("⚠️ Door object not found.");
        }
    }

    private void Update()
    {
        if (isPlayerNearby)
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("🟢 Player pressed E");

                    if (!doorOpened)
                    {
                        if (!hasAccess)
                        {
                            Debug.Log("🔍 Checking access before opening the door...");
                            CheckDoorAccess();
                        }
                        else
                        {
                            Debug.Log("✅ Access already verified – opening the door.");
                            OpenDoor();
                        }
                    }
                    else
                    {
                        Debug.Log("🔒 Closing the door.");
                        CloseDoor();
                    }
                }
            }
        }

        if (shouldMove && doorGameObject != null)
        {
            doorGameObject.transform.position = Vector3.MoveTowards(
                doorGameObject.transform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );

            if (Vector3.Distance(doorGameObject.transform.position, targetPosition) < 0.01f)
            {
                Debug.Log(doorOpened ? "✅ Door fully opened." : "✅ Door fully closed.");
                shouldMove = false;
            }
        }
    }

    private void CloseDoor()
    {
        if (doorGameObject != null)
        {
            Debug.Log("▶️ Starting door closing movement.");
            targetPosition = closedPosition;
            shouldMove = true;
            doorOpened = false;
        }
        else
        {
            Debug.LogWarning("⚠️ Door object not found for closing.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("🚶 Player entered near the door");
            isPlayerNearby = true;

            if (walletLogin == null)
            {
                walletLogin = other.GetComponentInChildren<WalletLogin>();
                if (walletLogin != null)
                {
                    Debug.Log("🔗 WalletLogin referenced from trigger. " + walletLogin.WalletAddress);
                }
                else
                {
                    Debug.LogError("❌ WalletLogin not found on player!");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("🚪 Player left the area near the door");
            isPlayerNearby = false;
        }
    }

    public void OnWalletConnectionFailed(string error)
    {
        Debug.LogError($"❌ MetaMask error: {error}");
    }
}
