using UnityEngine;

public enum PlantStage
{
    Dead,
    Small,
    Growing,
    FullyGrown,
    NeedsWater,
    NeedsMaintenance
}

public class Plant : MonoBehaviour
{
    [Header("Plant Settings")]
    public PlantData plantData;
    public PlantStage currentStage = PlantStage.Dead;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    
    private float growthTimer = 0f;
    private bool hasBeenWatered = false;
    private bool hasBeenMaintained = false;
    private static Sprite whiteSquareSprite; // Shared sprite for all plants
    
    void Awake()
    {
        // Create white square sprite if it doesn't exist
        if (whiteSquareSprite == null)
        {
            whiteSquareSprite = CreateWhiteSquareSprite();
        }
        
        // Ensure sprite renderer exists
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // Set the white square sprite
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = whiteSquareSprite;
        }
    }
    
    void Start()
    {
        if (plantData == null)
        {
            Debug.LogError("PlantData is missing!");
            return;
        }
        UpdateVisual();
    }
    
    void Update()
    {
        if (plantData == null) return;
        
        if (currentStage == PlantStage.Small || currentStage == PlantStage.Growing)
        {
            growthTimer += Time.deltaTime;
            
            if (currentStage == PlantStage.Small && growthTimer >= plantData.growthTime / 2f)
            {
                currentStage = PlantStage.NeedsWater;
                UpdateVisual();
            }
            else if (currentStage == PlantStage.Growing && growthTimer >= plantData.growthTime)
            {
                currentStage = PlantStage.FullyGrown;
                GameManager.Instance?.AddScore(plantData.pointValue);
                UpdateVisual();
            }
        }
    }
    
    public void Initialize(PlantData data)
    {
        plantData = data;
        currentStage = PlantStage.Small;
        growthTimer = 0f;
        hasBeenWatered = false;
        hasBeenMaintained = false;
        UpdateVisual();
    }
    
    public void Water()
    {
        if (currentStage == PlantStage.NeedsWater)
        {
            currentStage = PlantStage.Growing;
            hasBeenWatered = true;
            growthTimer = plantData.growthTime / 2f;
            UpdateVisual();
        }
    }
    
    public void Maintain()
    {
        if (currentStage == PlantStage.NeedsMaintenance)
        {
            currentStage = PlantStage.FullyGrown;
            hasBeenMaintained = true;
            GameManager.Instance?.AddScore(plantData.maintenanceBonus);
            UpdateVisual();
        }
    }
    
    public bool IsFullyGrown()
    {
        return currentStage == PlantStage.FullyGrown;
    }
    
    void UpdateVisual()
    {
        if (spriteRenderer == null || plantData == null) return;
        
        // Base color from plant data
        Color baseColor = plantData.plantColor;
        
        // Adjust brightness/hue based on stage
        Color stageColor = baseColor;
        float scale = 1f; // Scale factor for size
        
        switch (currentStage)
        {
            case PlantStage.Dead:
                // Very dark, small
                stageColor = baseColor * 0.2f;
                stageColor.a = 0.5f; // Semi-transparent
                scale = 0.3f;
                break;
            case PlantStage.Small:
                // Dim, small
                stageColor = baseColor * 0.5f;
                scale = 0.5f;
                break;
            case PlantStage.Growing:
            case PlantStage.NeedsWater:
                // Medium brightness, medium size
                stageColor = baseColor * 0.75f;
                scale = 0.75f;
                break;
            case PlantStage.FullyGrown:
            case PlantStage.NeedsMaintenance:
                // Full brightness, full size
                stageColor = baseColor;
                scale = 1f;
                break;
        }
        
        spriteRenderer.color = stageColor;
        transform.localScale = Vector3.one * scale;
    }
    
    // Creates a simple white square sprite programmatically
    static Sprite CreateWhiteSquareSprite()
    {
        int size = 64; // 64x64 pixels
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        // Fill with white
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from texture
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), // Pivot at center
            100f // Pixels per unit
        );
        
        return sprite;
    }
}