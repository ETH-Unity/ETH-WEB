using System.Runtime.InteropServices;
using UnityEngine;

public static class MetaMaskInterop
{
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ConnectWallet();

    [DllImport("__Internal")]
    private static extern void SendTransaction(string to, string data, string value);
    #endif
    
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CallContractFunction(string to, string data);
    #endif
    
    public static void Call(string to, string data)
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        CallContractFunction(to, data);
    #else
        Debug.Log($"CallContractFunction: {to}, data: {data}");
    #endif
    }


    public static void Connect()
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        ConnectWallet();
    #else
        Debug.Log("ConnectWallet() only works in WebGL builds.");
    #endif
    }

    public static void SendTx(string to, string data, string value = "0x0")
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        SendTransaction(to, data, value);
    #else
        Debug.Log($"SendTransaction: {to}, data: {data}, value: {value}");
    #endif
    }
    
}
