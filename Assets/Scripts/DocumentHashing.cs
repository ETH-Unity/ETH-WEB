using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DocumentHashing : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private TMP_InputField urlInputField;
    [SerializeField] private Button hashButton;
    [SerializeField] private TMP_Text resultText;

    [Header("Contract Settings")]
    [Tooltip("Address of the DocumentSigner contract")]
    [SerializeField] private string contractAddress = ""; // Set in Inspector

    // Callback names for MetaMaskPlugin.jslib
    private const string SIGN_SUCCESS_CALLBACK = nameof(OnSignSuccess);
    private const string SIGN_ERROR_CALLBACK = nameof(OnSignError);


#if UNITY_WEBGL && !UNITY_EDITOR

#else

#endif

    private void Awake()
    {
        if (hashButton != null)
            hashButton.onClick.AddListener(OnHashButtonClicked);
    }

    private void OnDestroy()
    {
        if (hashButton != null)
            hashButton.onClick.RemoveListener(OnHashButtonClicked);
    }


    private void OnHashButtonClicked()
    {
        if (urlInputField == null || string.IsNullOrWhiteSpace(urlInputField.text))
        {
            SetResultText("Please enter a valid URL.");
            return;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        MetaMaskInterop.HashFromUrl(urlInputField.text, gameObject.name, nameof(OnHashResult), nameof(OnHashError));
#else
        SetResultText("Hashing only works in WebGL build.");
#endif
    }


    // Store the last hash for use in OnSignSuccess
    private string _lastSignedHash;

    // Called by JS on success
    public void OnHashResult(string hash)
    {
        SetResultText($"SHA-256 Hash:\n{hash}\nRequesting signature...");
        _lastSignedHash = hash;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrEmpty(contractAddress) || contractAddress.Length != 42 || !contractAddress.StartsWith("0x"))
        {
            SetResultText("Invalid contract address. Please set it in the Inspector.");
            return;
        }
        // Prepare data for signDocument(bytes32)
        // Function selector: 0x166cba38
        // Argument: 32-byte hash, exactly 64 hex chars
        string h = hash.StartsWith("0x") ? hash.Substring(2) : hash;
        if (h.Length != 64)
        {
            SetResultText($"Hash must be 32 bytes (64 hex chars). Got: {h.Length}");
            return;
        }
        string data = "0x166cba38" + h;
        // Call SendTransaction (value = 0)
        MetaMaskInterop.SendTx(contractAddress, data, "0x0", gameObject.name, SIGN_SUCCESS_CALLBACK, SIGN_ERROR_CALLBACK);
#else
        SetResultText($"Hash: {hash}\nSigning only works in WebGL build.");
#endif
    }

    // Called by JS on error
    public void OnHashError(string error)
    {
        SetResultText($"Hashing error: {error}");
    }

    // Called by JS on successful signature transaction confirmation
    public void OnSignSuccess(string json)
    {
        // json is expected to be: { txHash: ..., from: ... }
        string txHash = null;
        string walletAddress = null;
        try
        {
            var parsed = JsonUtility.FromJson<SignResult>(json);
            txHash = parsed.txHash;
            walletAddress = parsed.from;
        }
        catch
        {
            // fallback: treat as plain txHash
            txHash = json;
        }
        SetResultText($"Document signed!\nTx Hash: {txHash}");

        // Use the last hash if available
        string lastHash = _lastSignedHash;
        if (!string.IsNullOrEmpty(walletAddress) && !string.IsNullOrEmpty(lastHash) && ChatManager.Singleton != null)
        {
            ChatManager.Singleton.NotifyDocumentSigned(walletAddress, lastHash);
        }
    }

    [System.Serializable]
    private class SignResult
    {
        public string txHash;
        public string from;
    }

    // Called by JS on signature error
    public void OnSignError(string error)
    {
        SetResultText($"Signature error: {error}");
    }

    private void SetResultText(string text)
    {
        if (resultText != null)
            resultText.text = text;
    }
}
