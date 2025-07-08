using System.Runtime.InteropServices;
using UnityEngine;

public static class MetaMaskInterop
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ConnectWallet();

    [DllImport("__Internal")]
    private static extern void CallContractFunction(string to, string data);

    [DllImport("__Internal")]
    private static extern void FetchBalanceJS(string address);

    [DllImport("__Internal")]
    private static extern void SendTransaction(string to, string data, string value, string objectName, string successCallback, string errorCallback);
    
    [DllImport("__Internal")]
    private static extern void InitiateTransferJS(string to, string amount, string message, string contractAddress);

    [DllImport("__Internal")]
    private static extern void SignTransferJS(string contractAddress);

    [DllImport("__Internal")]
    private static extern void HashFileFromUrl(string url, string objectName, string callback, string errorCallback);

#endif

    public static void Connect()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ConnectWallet();
#else
        Debug.Log("🧪 MetaMask Connect() toimii vain WebGL buildissä.");
#endif
    }

    public static void Call(string contractAddress, string callData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CallContractFunction(contractAddress, callData);
#else
        Debug.Log($"🧪 Call: {contractAddress}, data: {callData}");
#endif
    }

    public static void FetchBalance(string address)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FetchBalanceJS(address);
#else
        Debug.Log("🧪 FetchBalance toimii vain WebGL buildissä.");
#endif
    }

    public static void SendTx(string to, string data, string value, string objectName, string successCallback, string errorCallback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SendTransaction(to, data, value, objectName, successCallback, errorCallback);
#else
        Debug.Log($"🧪 SendTransaction: {to}, data: {data}, value: {value}, objectName: {objectName}, successCallback: {successCallback}, errorCallback: {errorCallback}");
#endif
    }

    public static void InitiateTransfer(string to, string amount, string message, string contractAddress)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitiateTransferJS(to, amount, message, contractAddress);
#else
        Debug.Log($"[Editor] MetaMaskInterop.InitiateTransfer not available: to={to}, amount={amount}, contractAddress={contractAddress}");
#endif
    }

    public static void SignTransfer(string contractAddress)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SignTransferJS(contractAddress);
#else
        Debug.Log($"[Editor] MetaMaskInterop.SignTransfer not available: contractAddress={contractAddress}");
#endif
    }
    
    public static void HashFromUrl(string url, string objectName, string callback, string errorCallback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HashFileFromUrl(url, objectName, callback, errorCallback);
#endif
    }
}