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
    [SerializeField] private Text instructionText;

   [Header("Visibility")]
    [Tooltip("When off, hides only the score label. Tool selection UI is always shown. If GardenManager creates this object at runtime, it sets this from GardenManager > Show Score In UI.")]
    [SerializeField] private bool showScoreUI = false;

    private Canvas canvas;

    /// <summary>Sets score label visibility (used when GardenManager spawns GameUI at runtime).</summary>
    public void SetShowScoreUI(bool show)
    {
        showScoreUI = show;
        ApplyScoreVisibility();
    }

    private void Start()
    {
        if (playerTools == null) playerTools = Object.FindFirstObjectByType<PlayerTools>();
        if (gardenManager == null) gardenManager = Object.FindFirstObjectByType<GardenManager>();

        if (toolText == null || scoreText == null)
            CreateUI();

        ApplyScoreVisibility();
    }

    private void OnValidate()
    {
        ApplyScoreVisibility();
    }

    private void ApplyScoreVisibility()
    {
        if (scoreText == null) return;
        scoreText.gameObject.SetActive(showScoreUI);
    }

    private void Update()
    {
        if (toolText != null && playerTools != null)
        {
            string toolName = GetToolDisplayName(playerTools.CurrentTool);
            if (playerTools.CurrentTool == ToolType.SeedPacket && playerTools.SelectedSeedType != null)
                toolName += $" ({playerTools.SelectedSeedType.displayName})";
            if (instructionText != null && playerTools != null)
                instructionText.text = GetToolInstructions(playerTools.CurrentTool);
            toolText.text = $"Tool: {toolName}";
        }

        if (showScoreUI && scoreText != null && gardenManager != null)
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

    private string GetToolInstructions(ToolType tool)
    {
        return tool switch
        {
            ToolType.Shovel => "Use the Red button to get rid of dead plants.",
            ToolType.SeedPacket => "Use the Red button to plant a seed.",
            ToolType.WateringCan => "Use the Red button to water a plant.",
            _ => ""
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
