using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class OnScreenDebugLogger : MonoBehaviour
{
    [Header("Bật/tắt log trên màn hình")]
    [SerializeField] private bool enableOnScreenLog = true;

    [Header("Cấu hình hiển thị")]
    [SerializeField] private int maxLines = 15;
    [SerializeField] private int fontSize = 28;

    private readonly List<string> logLines = new List<string>();
    private string fullLogText = "";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!enableOnScreenLog) return;

        string color = type switch
        {
            LogType.Error => "red",
            LogType.Exception => "red",
            LogType.Warning => "yellow",
            _ => "white"
        };

        logLines.Add($"<color={color}>[{type}] {logString}</color>");

        if (logLines.Count > maxLines)
            logLines.RemoveAt(0);

        var sb = new StringBuilder();
        foreach (var line in logLines)
            sb.AppendLine(line);
        fullLogText = sb.ToString();
    }

    void OnGUI()
    {
        if (!enableOnScreenLog) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            richText = true,
            wordWrap = true
        };

        GUI.Box(new Rect(10, 10, Screen.width - 20, Screen.height * 0.4f), "");
        GUI.Label(new Rect(20, 20, Screen.width - 40, Screen.height * 0.4f - 20), fullLogText, style);
    }
}