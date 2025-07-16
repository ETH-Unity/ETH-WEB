using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NFTDeployer : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DeployCertificateNFT(string abi, string bytecode, string name, string symbol);

    [DllImport("__Internal")]
    private static extern void MintCertificateNFT(string contractAddress, string abi, string recipient, string data);
#endif

    [Header("UI Elements")]
    public Button deployButton;
    public Button mintButton;
    public TMP_InputField recipientAddressInput;
    public TMP_InputField certificateDataInput;
    public TMP_InputField contractNameInput;
    public TMP_InputField contractSymbolInput;

    public TMP_Text statusText;

    [Header("Smart Contract Files")]
    public TextAsset abiJson;
    public TextAsset bytecodeText;

    [Header("Contract Settings")]
    [Tooltip("Pre-deployed contract address to use for minting")]
    public string contractAddress; 

    void Start()
    {
        if (deployButton != null) deployButton.onClick.AddListener(OnDeployClicked);
        if (mintButton != null) mintButton.onClick.AddListener(OnMintClicked);
        // Set contract address from config if not set in inspector
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.NFTContractAddress;
        }
    }

    void OnDeployClicked()
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        if (abiJson == null || bytecodeText == null)
        {
            statusText.text = "Missing ABI or Bytecode file.";
            return;
        }
    
        string contractName = contractNameInput?.text.Trim();
        string contractSymbol = contractSymbolInput?.text.Trim();
    
        if (string.IsNullOrEmpty(contractName) || string.IsNullOrEmpty(contractSymbol))
        {
            statusText.text = "Please enter contract name and symbol.";
            return;
        }
    
        DeployCertificateNFT(abiJson.text, bytecodeText.text, contractName, contractSymbol);
        statusText.text = "Deploying contract...";
    #else
        Debug.Log("Deployment only works in WebGL builds.");
    #endif
    }


    void OnMintClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (abiJson == null)
        {
            statusText.text = "Missing ABI file.";
            return;
        }

        string recipient = recipientAddressInput?.text.Trim();
        string data = certificateDataInput?.text.Trim();

        if (string.IsNullOrEmpty(contractAddress) || string.IsNullOrEmpty(recipient) || string.IsNullOrEmpty(data))
        {
            statusText.text = "Please fill all required fields.";
            return;
        }

        MintCertificateNFT(contractAddress, abiJson.text, recipient, data);
        statusText.text = "Minting NFT...";
#else
        Debug.Log("Minting only works in WebGL builds.");
#endif
    }
}
