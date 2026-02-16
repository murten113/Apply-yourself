using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays tool selection and score. Add to a GameObject in the scene.
/// Creates a simple Canvas with text if references are not assigned.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerTools playerTools;
    [SerializeField] private GardenManager gardenManager;

    [Header("UI Elements (optional - will create if null)")]
    [SerializeField] private Text toolText;
    [SerializeField] private Text scoreText;

    private Canvas canvas;

    private void Start()
    {
        if (playerTools == null) playerTools = FindObjectOfType<PlayerTools>();
        if (gardenManager == null) gardenManager = FindObjectOfType<GardenManager>();

        if (toolText == null || scoreText == null)
            CreateUI();
    }

    private void Update()
    {
        if (toolText != null && playerTools != null)
        {
            string toolName = GetToolDisplayName(playerTools.CurrentTool);
            if (playerTools.CurrentTool == ToolType.SeedPacket && playerTools.SelectedSeedType != null)
                toolName += $" ({playerTools.SelectedSeedType.displayName})";
            toolText.text = $"Tool: {toolName}";
        }

        if (scoreText != null && gardenManager != null)
            scoreText.text = $"Score: {gardenManager.Score}";
    }

    private string GetToolDisplayName(ToolType tool)
    {
        return tool switch
        {
            ToolType.Shovel => "Shovel",
            ToolType.SeedPacket => "Seed Packet",
            ToolType.WateringCan => "Watering Can",
            _ => tool.ToString()
        };
    }

    private void CreateUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject toolObj = new GameObject("ToolText");
        toolObj.transform.SetParent(transform, false);
        toolText = toolObj.AddComponent<Text>();
        toolText.text = "Tool: Shovel";
        toolText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        toolText.fontSize = 24;
        toolText.color = Color.white;
        RectTransform toolRect = toolText.rectTransform;
        toolRect.anchorMin = new Vector2(0, 1);
        toolRect.anchorMax = new Vector2(0, 1);
        toolRect.pivot = new Vector2(0, 1);
        toolRect.anchoredPosition = new Vector2(20, -20);
        toolRect.sizeDelta = new Vector2(400, 40);

        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(transform, false);
        scoreText = scoreObj.AddComponent<Text>();
        scoreText.text = "Score: 0";
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 24;
        scoreText.color = Color.white;
        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.anchorMin = new Vector2(1, 1);
        scoreRect.anchorMax = new Vector2(1, 1);
        scoreRect.pivot = new Vector2(1, 1);
        scoreRect.anchoredPosition = new Vector2(-20, -20);
        scoreRect.sizeDelta = new Vector2(150, 40);
    }
}
