using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{

    public static ChatManager Singleton;

    public ScrollRect scrollRect;
    public Transform chatContent;
    public TMP_InputField chatInputField;
    public Image chatBackground;
    public CanvasGroup chatCanvasGroup;
    public CanvasGroup chatInputCanvasGroup;

    public string PlayerName;
    private bool chatOpen = false;
    private PlayerController localPlayer;
    public static bool IsAnyMenuOpen = false;

    void Awake()
    {
        Singleton = this;
        // PlayerName will be set later by the PlayerController
        if (string.IsNullOrEmpty(PlayerName))
            PlayerName = "Player"; // Set a default name initially

        // Hide chat input by default using its CanvasGroup
        if (chatInputCanvasGroup != null)
        {
            chatInputCanvasGroup.alpha = 0;
            chatInputCanvasGroup.interactable = false;
            chatInputCanvasGroup.blocksRaycasts = false;
        }

        chatInputField.gameObject.SetActive(true); // Keep the GameObject active
    }

    public void RegisterLocalPlayer(PlayerController player)
    {
        localPlayer = player;
        if (localPlayer != null && localPlayer.IsOwner)
        {
            string walletAddr = localPlayer.walletAddress.Value.ToString();
            if (!string.IsNullOrEmpty(walletAddr))
            {
                PlayerName = FormatAddress(walletAddr);
            }
            else if (localPlayer.overheadNameText != null && !string.IsNullOrEmpty(localPlayer.overheadNameText.text))
            {
                PlayerName = localPlayer.overheadNameText.text; // Fallback to overhead text
            }
        }
    }

    public void UpdateLocalPlayerName(string address)
    {
        if (!string.IsNullOrEmpty(address))
        {
            PlayerName = FormatAddress(address);
        }
    }

    void Update()
    {
        // Set chat panel opacity based on chatOpen
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = chatOpen ? 1.0f : 0.4f; // 1.0 when active, 0.4 when inactive
        }

        // Open chat input with Enter if not open, not focused, and no other UI is open
        if (!chatOpen && Input.GetKeyDown(KeyCode.Return) && !IsAnyMenuOpen)
        {
            chatOpen = true;
            chatInputField.text = "";
            if (chatInputCanvasGroup != null)
            {
                chatInputCanvasGroup.alpha = 1;
                chatInputCanvasGroup.interactable = true;
                chatInputCanvasGroup.blocksRaycasts = true;
            }
            chatInputField.ActivateInputField();
            chatInputField.Select();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (localPlayer != null)
            {
                localPlayer.SetCameraRotationEnabled(false);
                localPlayer.IsMenuOpen = true; // Block movement for local player only
            }
        }
        // If chat is open
        else if (chatOpen)
        {
            // Send message with Enter (keep chat open)
            if (Input.GetKeyDown(KeyCode.Return))
            {
                string text = chatInputField.text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    SendChatMessage(text, PlayerName);
                }
                chatInputField.text = "";
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
            // Cancel chat with Escape (close chat input only)
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                chatInputField.text = "";
                chatInputField.DeactivateInputField();
                if (chatInputCanvasGroup != null)
                {
                    chatInputCanvasGroup.alpha = 0;
                    chatInputCanvasGroup.interactable = false;
                    chatInputCanvasGroup.blocksRaycasts = false;
                }
                chatOpen = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (localPlayer != null)
                {
                    localPlayer.SetCameraRotationEnabled(true);
                    localPlayer.IsMenuOpen = false; // Unblock movement for local player only
                }
            }
            if (localPlayer != null && localPlayer.IsOwner)
            {
                localPlayer.SetCameraRotationEnabled(false); // Disable camera rotation while chat is open
                localPlayer.IsMenuOpen = true; // Ensure movement stays blocked while chat is open
            }
        }
    }

    public void AddMessage(string message)
    {
        if (chatContent == null)
        {
            Debug.LogError("[ChatManager] chatContent not assigned!");
            return;
        }

        // Create a new GameObject to hold the message text
        GameObject messageObject = new GameObject("ChatMessage");
        messageObject.transform.SetParent(chatContent, false);

        // Add and configure the TextMeshProUGUI component
        TextMeshProUGUI textComponent = messageObject.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.textWrappingMode = TextWrappingModes.Normal;

        // Find URLs in the message and wrap them in TMP link tags
        string pattern = @"(https?://[\w\d./?=#&%\-]+)";
        string parsedMessage = Regex.Replace(message, pattern, m => $"<link=\"{m.Value}\"><color=#58A6FF><u>{m.Value}</u></color></link>");
        textComponent.text = parsedMessage;

        // Add the NESTED link handler script to make the links clickable
        messageObject.AddComponent<LinkClickHandler>();

        // Add a LayoutElement to allow the VerticalLayoutGroup to control the height
        messageObject.AddComponent<LayoutElement>();

        // Scroll to the bottom to show the latest message
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait for the end of the frame to ensure the layout is updated
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void SendChatMessage(string message, string playerName = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string sender = playerName;
        if (string.IsNullOrEmpty(sender))
            sender = PlayerName;
        if (string.IsNullOrEmpty(sender))
            sender = "Player";

        string S = sender + ": " + message;
        SendChatMessageServerRpc(S);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        ReceiveChatMessageClientRPC(message);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRPC(string message)
    {
        Singleton.AddMessage(message);
    }

    private string FormatAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 8)
            return address;
        return $"{address.Substring(0, 5)}...{address.Substring(address.Length - 3)}";
    }


    // Sends an automated system message to the chat. Use for notifications like document signing, etc.
    public void SendAutomatedMessage(string template, params object[] args)
    {
        string msg = string.Format(template, args);
        string systemMsgPrefix = "<color=#ffa500>SYSTEM</color>";
        SendChatMessage(msg, systemMsgPrefix);
    }
    
    public void NotifyDocumentSigned(string walletAddress, string hash)
    {
        string formattedWallet = FormatAddress(walletAddress);
        SendAutomatedMessage("Wallet {0} signed document with hash: {1}", formattedWallet, hash);
    }
    

    // --- Nested Class for Link Handling ---
    // This component will be added to each chat message object to handle clicks.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI _textMeshPro;
        private Camera _canvasCamera;

        void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
            if (_textMeshPro.canvas != null && _textMeshPro.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _canvasCamera = _textMeshPro.canvas.worldCamera;
            }
            else
            {
                _canvasCamera = null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, eventData.position, _canvasCamera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];
                Application.OpenURL(linkInfo.GetLinkID());
            }
        }
    }

}
