using UnityEngine;

public class DoorAccessBridgeHandler : MonoBehaviour
{
    public static DoorAccessBridgeHandler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ DoorAccessBridgeHandler is active.");
        }
        else
        {
            Debug.LogWarning("⚠️ Duplicate DoorAccessBridgeHandler found, destroying...");
            Destroy(gameObject);
        }
    }


    // Tämä metodi kutsutaan JavaScriptistä .jslib:in kautta
    public void HandleDoorAccessResult(string result)
    {
        Debug.Log("📡 Bridge received result from MetaMask: " + result);
    
        if (DoorAccessBridge.CurrentTarget != null)
        {
            Debug.Log("➡️ Forwarding result to: " + DoorAccessBridge.CurrentTarget.gameObject.name);
            DoorAccessBridge.CurrentTarget.OnDoorAccessResult(result);
        }
        else
        {
            Debug.LogWarning("⚠️ No current DoorAccessController target set in DoorAccessBridge.");
        }
    }

}
