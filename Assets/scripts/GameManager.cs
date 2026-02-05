using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    public float gameTimer = 300f; // 5 minutes default
    public int score = 0;
    
    [Header("Tree Settings")]
    public Transform centerTree;
    public Vector3 treeStartScale = new Vector3(0.3f, 0.3f, 1f);
    public Vector3 treeMaxScale = new Vector3(1.5f, 1.5f, 1f);
    public int maxScoreForTree = 1000;
    
    [Header("Garden Plots")]
    public List<GardenPlot> gardenPlots = new List<GardenPlot>();
    public int plotsUnlocked = 3; // Start with 3 plots

    
    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Image beforeImage;
    public Image afterImage;
    
    [Header("Tools")]
    public ToolType currentTool = ToolType.Shovel;
    public PlantData selectedPlantType; // Selected plant for planting
    
    [Header("Available Plant Types")]
    public List<PlantData> availablePlantTypes = new List<PlantData>();
    
    [Header("Tool Selection Visual Feedback (Optional)")]
    public Button shovelButton;
    public Button seedPacketButton;
    public Button wateringCanButton;
    
    [Header("Plant Selection Visual Feedback (Optional)")]
    public Button yellowFlowerButton;
    public Button redFlowerButton;
    public Button purpleFlowerButton;
    
    private bool gameActive = true;
    private float currentTime;
    private Camera mainCamera;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        currentTime = gameTimer;
        mainCamera = Camera.main;
        UpdateUnlockedPlots();
        
        // Set default selected plant if available
        if (selectedPlantType == null && availablePlantTypes.Count > 0)
        {
            selectedPlantType = availablePlantTypes[0];
        }
        
        // Debug check for button assignments
        if (shovelButton == null)
            Debug.LogWarning("Shovel button not assigned in GameManager!");
        if (wateringCanButton == null)
            Debug.LogWarning("Watering Can button not assigned in GameManager!");
        
        // Update button visuals
        UpdateToolButtonVisuals();
        
        // Auto-find garden plots if list is empty
        if (gardenPlots.Count == 0)
        {
            gardenPlots.AddRange(FindObjectsOfType<GardenPlot>());
            Debug.Log($"Auto-found {gardenPlots.Count} garden plots");
        }
    }
    
    void Update()
    {
        if (!gameActive) return;
        
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 0)
        {
            currentTime = 0;
            EndGame();
        }
        
        UpdateUI();
        UpdateTreeScale();
        HandleInput();
    }
    
    void HandleInput()
    {
        // Only handle clicks if not clicking on UI
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            // Use ContactFilter2D to check ALL layers
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter(); // Check all layers
            
            Collider2D[] results = new Collider2D[10];
            int count = Physics2D.OverlapPoint(mousePos, filter, results);
            
            if (count > 0)
            {
                Debug.Log($"Found {count} collider(s) at position {mousePos}");
                for (int i = 0; i < count; i++)
                {
                    Debug.Log($"  Collider {i}: {results[i].name} on layer {results[i].gameObject.layer}");
                    GardenPlot plot = results[i].GetComponent<GardenPlot>();
                    if (plot != null)
                    {
                        Debug.Log($"Clicked on plot: {plot.name}");
                        UseToolOnPlot(plot);
                        return; // Found plot, exit
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No collider at mouse position: {mousePos}. Camera Z: {mainCamera.transform.position.z}");
                Debug.LogWarning($"Screen position: {Input.mousePosition}, World position: {mousePos}");
            }
        }
    }
    
    // Check if mouse is over UI element
    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    void UseToolOnPlot(GardenPlot plot)
    {
        switch (currentTool)
        {
            case ToolType.Shovel:
                Debug.Log("Using shovel on plot");
                plot.RemoveDeadPlant();
                break;
            case ToolType.SeedPacket:
                if (selectedPlantType != null)
                {
                    plot.PlantSeed(selectedPlantType);
                }
                else
                {
                    Debug.LogWarning("No plant type selected!");
                }
                break;
            case ToolType.WateringCan:
                plot.WaterPlant();
                break;
        }
    }
    
    // Button methods for tool selection
    public void SelectShovel()
    {
        currentTool = ToolType.Shovel;
        UpdateToolButtonVisuals();
        Debug.Log("Selected Shovel");
    }
    
    public void SelectWateringCan()
    {
        currentTool = ToolType.WateringCan;
        UpdateToolButtonVisuals();
        Debug.Log("Selected Watering Can");
    }
    
    // Update button visuals to show which tool is selected
    void UpdateToolButtonVisuals()
    {
        if (shovelButton != null)
        {
            var colors = shovelButton.colors;
            colors.normalColor = (currentTool == ToolType.Shovel) ? Color.green : Color.white;
            colors.selectedColor = (currentTool == ToolType.Shovel) ? Color.green : Color.white;
            colors.highlightedColor = (currentTool == ToolType.Shovel) ? Color.green : new Color(0.96f, 0.96f, 0.96f);
            shovelButton.colors = colors;
        }
        
        if (seedPacketButton != null)
        {
            var colors = seedPacketButton.colors;
            colors.normalColor = (currentTool == ToolType.SeedPacket) ? Color.green : Color.white;
            colors.selectedColor = (currentTool == ToolType.SeedPacket) ? Color.green : Color.white;
            colors.highlightedColor = (currentTool == ToolType.SeedPacket) ? Color.green : new Color(0.96f, 0.96f, 0.96f);
            seedPacketButton.colors = colors;
        }
        
        if (wateringCanButton != null)
        {
            var colors = wateringCanButton.colors;
            colors.normalColor = (currentTool == ToolType.WateringCan) ? Color.green : Color.white;
            colors.selectedColor = (currentTool == ToolType.WateringCan) ? Color.green : Color.white;
            colors.highlightedColor = (currentTool == ToolType.WateringCan) ? Color.green : new Color(0.96f, 0.96f, 0.96f);
            wateringCanButton.colors = colors;
        }
    }
    
    public void AddScore(int points)
    {
        score += points;
        BackdropManager.Instance?.CheckScoreThresholds(score);
    }
    
    void UpdateTreeScale()
    {
        if (centerTree == null) return;
        
        float progress = Mathf.Clamp01((float)score / maxScoreForTree);
        centerTree.localScale = Vector3.Lerp(treeStartScale, treeMaxScale, progress);
    }
    
    void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    public void UnlockNewPlots()
    {
        int fullyGrownCount = 0;
        foreach (var plot in gardenPlots)
        {
            if (plot.IsFullyGrown())
            {
                fullyGrownCount++;
            }
        }
        
        int newPlotsUnlocked = Mathf.Min(fullyGrownCount / 2, gardenPlots.Count);
        if (newPlotsUnlocked > plotsUnlocked)
        {
            plotsUnlocked = newPlotsUnlocked;
            UpdateUnlockedPlots();
        }
    }
    
    void UpdateUnlockedPlots()
    {
        for (int i = 0; i < gardenPlots.Count; i++)
        {
            gardenPlots[i].SetUnlocked(i < plotsUnlocked);
        }
    }
    
    void EndGame()
    {
        gameActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}";
        }
        // TODO: Capture before/after screenshots
    }
}

public enum ToolType
{
    Shovel,
    SeedPacket,
    WateringCan
}