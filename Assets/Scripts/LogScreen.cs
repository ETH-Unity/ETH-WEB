using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class LogScreen : MonoBehaviour
{
    public TMP_Text logText; 
    [SerializeField] private string contractAddress = "";
    private List<string> eventLogs = new List<string>(); 
    private bool isFirstFetch = true; 
    private bool isFetching = false; 
    private const float textAreaWidth = 100f; 
    private const float textAreaHeight = 300f; 

    [DllImport("__Internal")]
    private static extern void FetchEvents(string contractAddress);

    private void Start()
    {
        // Use config value if inspector field is empty
        if (string.IsNullOrWhiteSpace(contractAddress) && ClientConfigLoader.Config != null)
        {
            contractAddress = ClientConfigLoader.Config.UserDeviceContractAddress;
        }
        Debug.Log($"[LogScreen] contractAddress in use: {contractAddress}");
        if (logText == null)
        {
            Debug.LogError("logText is not assigned!");
        }
        else
        {
            // Set up text component
            logText.rectTransform.sizeDelta = new Vector2(textAreaWidth, textAreaHeight); 
            logText.rectTransform.anchorMin = new Vector2(0f, 0f); 
            logText.rectTransform.anchorMax = new Vector2(0f, 1f); 
            logText.rectTransform.pivot = new Vector2(0f, 1f); 
            logText.rectTransform.anchoredPosition = Vector2.zero; 
            logText.enableWordWrapping = true; 
            logText.overflowMode = TextOverflowModes.Overflow; 
            logText.alignment = TextAlignmentOptions.TopLeft; 
            logText.enableAutoSizing = false; 
            logText.text = ""; 
        }
        StartCoroutine(UpdateLogsCoroutine());
    }

    // Method to trigger FetchEvents from JavaScript
    public void TriggerFetchEvents()
    {
        StartCoroutine(UpdateLogsCoroutine());
    }

    private IEnumerator UpdateLogsCoroutine() // Update the logs
    {
        while (true)
        {
            if (!Application.isPlaying) yield break;
            if (isFirstFetch && !isFetching)
            {
                eventLogs.Clear();
                isFirstFetch = false;
            }
            if (!isFetching)
            {
                isFetching = true;
                FetchEvents(contractAddress);
            }
            yield return new WaitForSeconds(10f);
            isFetching = false;
        }
    }

    public void ReceiveEvents(string jsonLogs) // Receive events from the blockchain to a string
    {
        try
        {
            string[] logs = JsonHelper.FromJson<string>(jsonLogs);
            eventLogs.Clear();
            if (logs != null && logs.Length > 0)
            {
                eventLogs.AddRange(logs);
            }
            else
            {
                eventLogs.Add("No logs received. Change account accesses to display the events here. ");
            }
            UpdateLogDisplay();
        }
        catch (System.Exception ex)
        {
            eventLogs.Clear();
            eventLogs.Add("Error processing events: " + ex.Message);
            UpdateLogDisplay();
        }
    }

    private void UpdateLogDisplay() // Update the LogScreen display
    {
        if (logText == null)
        {
            return;
        }
        string coloredText = "";
        for (int i = 0; i < eventLogs.Count; i++)
        {
            string log = eventLogs[i];
            string colorTag = GetColorTag(log);
            coloredText += $"{colorTag}{log}</color>\n";
        }
        logText.text = coloredText.TrimEnd('\n');
        logText.rectTransform.sizeDelta = new Vector2(textAreaWidth, textAreaHeight); 
        logText.ForceMeshUpdate(); 
    }

    private string GetColorTag(string log) //  Colours for the different events
    {
        if (log.StartsWith("AccessGranted")) return "<color=#FFFF00>";
        else if (log.StartsWith("AccessRevoked")) return "<color=#FF0000>";
        else if (log.StartsWith("AccessChanged")) return "<color=#0000FF>";
        return "<color=#FFFFFF>";
    }
}

// Custom helper class to deserialize JSON arrays
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrapper = "{\"array\":" + json + "}";
        Wrapper<T> wrapperObject = JsonUtility.FromJson<Wrapper<T>>(wrapper);
        return wrapperObject.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}