using UnityEngine;
using TMPro;

// Displays a list of players with valid addresses to enable useruser interactions

public class PlayerListUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject addressItemPrefab;

    public System.Action<string> OnAddressSelected; // Callback for selection

    void OnEnable()
    {
        RefreshPlayerList();
    }
    public void RefreshPlayerList()
    {
        if (contentParent == null || addressItemPrefab == null)
        {
            Debug.LogError("[PlayerListUI] ContentParent or AddressItemPrefab not assigned!");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        PlayerController localPlayer = null;
        // Use FindObjectsByType instead of the obsolete FindObjectsOfType
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        // Attempt to find the PlayerController that owns this specific UI instance
        // This assumes PlayerListUI is a child of the player prefab.
        PlayerController owningPlayerController = GetComponentInParent<PlayerController>();

        if (owningPlayerController != null && owningPlayerController.IsOwner)
        {
            localPlayer = owningPlayerController;
        }
        else
        {
            // Fallback: if not directly parented to an owner, or for a more global list context,
            // find the game's local player (the one whose client instance is running this code).
            foreach (var player in allPlayers)
            {
                if (player.IsOwner)
                {
                    localPlayer = player;
                    break;
                }
            }
        }

        foreach (var player in allPlayers)
        {
            // Only show players with a valid Ethereum address (0x... and 42 chars)
            string address = player.walletAddress.Value.ToString();
            bool isValidAddress = !string.IsNullOrEmpty(address) && address.Length == 42 && address.StartsWith("0x");
            if (!isValidAddress)
                continue;
            // Do not list the local player in their own list of "other" players to select.
            if (localPlayer != null && player == localPlayer)
                continue;

            string displayText = address.Substring(0, 5) + "..." + address.Substring(address.Length - 3);
            var item = Instantiate(addressItemPrefab, contentParent);
            var text = item.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = displayText;
            var button = item.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                string capturedAddress = address;
                button.onClick.AddListener(() => OnAddressSelected?.Invoke(capturedAddress));
                button.interactable = true;
            }
        }
    }
}