using System.Runtime.InteropServices;
using UnityEngine;

public static class MetaMaskInterop
{
    [DllImport("__Internal")]
    private static extern void ConnectWallet();

    public static void Connect()
    {
        ConnectWallet();
    }
}
