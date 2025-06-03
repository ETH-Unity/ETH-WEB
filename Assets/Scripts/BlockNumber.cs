using Nethereum.Unity.Rpc;
using UnityEngine;
using System.Collections;

public class BlockNumber : MonoBehaviour
{
    private string Url = "http://localhost:8545/";

    void Start()
    {
        StartCoroutine(GetLatestBlockNumber());
    }

    private IEnumerator GetLatestBlockNumber()
    {
        var rpcFactory = new UnityWebRequestRpcClientFactory(Url);
        var blockNumberRequest = new EthBlockNumberUnityRequest(rpcFactory);

        yield return blockNumberRequest.SendRequest();

        if (blockNumberRequest.Exception != null)
        {
            Debug.LogError($"[Nethereum] Error: {blockNumberRequest.Exception.Message}");
        }
        else
        {
            var blockNumber = blockNumberRequest.Result.Value;
            Debug.Log($"[Nethereum] Latest Block Number: {blockNumber}");
        }
    }
}