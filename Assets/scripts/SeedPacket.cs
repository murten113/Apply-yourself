using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SeedPacket : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Seed Packet Settings")]
    public PlantData plantData; // The plant type this seed packet represents
    
    [Header("Visual")]
    public Image seedPacketImage;
    public Color normalColor = Color.white;
    public Color draggingColor = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent when dragging
    
    [Header("Drop Detection")]
    public float maxDropDistance = 10f; // Maximum distance to consider a valid drop (increased for coordinate issues)
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private bool wasDroppedOnPlot = false;
    private Camera mainCamera;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        
        // Add CanvasGroup for raycast blocking during drag
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Store original position
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }
    
    void Start()
    {
        // Set visual color based on plant type
        if (seedPacketImage == null)
        {
            seedPacketImage = GetComponent<Image>();
        }
        
        if (seedPacketImage != null && plantData != null)
        {
            seedPacketImage.color = plantData.plantColor;
        }
        
        // Ensure we have a camera reference
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        wasDroppedOnPlot = false;
        canvasGroup.alpha = 0.7f; // Make semi-transparent
        canvasGroup.blocksRaycasts = false; // Allow raycasts to pass through
        
        // Move to top of hierarchy so it appears above other UI
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // Move the seed packet with the mouse
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f; // Restore full opacity
        canvasGroup.blocksRaycasts = true; // Re-enable raycasts
        
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            rectTransform.anchoredPosition = originalPosition;
            transform.SetParent(originalParent);
            return;
        }
        
        // Convert screen position to world position (same method as GameManager click detection)
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0; // Ensure z is 0 for 2D (same as click detection)
        
        Debug.Log($"Drop position - Screen: {Input.mousePosition}, World: {worldPos}, Camera Z: {mainCamera.transform.position.z}");
        
        // Use ContactFilter2D to check ALL layers
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // Check all layers
        
        Collider2D[] results = new Collider2D[10];
        GardenPlot targetPlot = null;
        
        // Method 1: OverlapPoint with all layers (same as click detection)
        int count = Physics2D.OverlapPoint(worldPos, filter, results);
        if (count > 0)
        {
            Debug.Log($"[DROP] Found {count} collider(s) at drop point {worldPos}");
            for (int i = 0; i < count; i++)
            {
                Debug.Log($"  [DROP] Collider {i}: {results[i].name} on layer {results[i].gameObject.layer}");
                GardenPlot plot = results[i].GetComponent<GardenPlot>();
                if (plot != null)
                {
                    targetPlot = plot;
                    Debug.Log($"[DROP] ✓ Found GardenPlot via collider: {targetPlot.name}");
                    break;
                }
                else
                {
                    Debug.Log($"  [DROP] Collider {results[i].name} has no GardenPlot component");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[DROP] No colliders found at drop point {worldPos}");
        }
        
        // Method 2: Try with radius if point check failed
        if (targetPlot == null)
        {
            count = Physics2D.OverlapCircle(worldPos, 1f, filter, results);
            if (count > 0)
            {
                Debug.Log($"[DROP] Found {count} collider(s) in radius 1");
                for (int i = 0; i < count; i++)
                {
                    GardenPlot plot = results[i].GetComponent<GardenPlot>();
                    if (plot != null)
                    {
                        targetPlot = plot;
                        Debug.Log($"[DROP] ✓ Found GardenPlot via radius 1: {targetPlot.name}");
                        break;
                    }
                }
            }
        }
        
        // Method 3: Try larger radius
        if (targetPlot == null)
        {
            count = Physics2D.OverlapCircle(worldPos, 2f, filter, results);
            if (count > 0)
            {
                Debug.Log($"[DROP] Found {count} collider(s) in radius 2");
                for (int i = 0; i < count; i++)
                {
                    GardenPlot plot = results[i].GetComponent<GardenPlot>();
                    if (plot != null)
                    {
                        targetPlot = plot;
                        Debug.Log($"[DROP] ✓ Found GardenPlot via radius 2: {targetPlot.name}");
                        break;
                    }
                }
            }
        }
        
        // Method 5: Fallback - Find closest GardenPlot from GameManager's list
        // Use world positions to handle parented GameObjects correctly
        if (targetPlot == null && GameManager.Instance != null)
        {
            Debug.Log($"[DROP] Using fallback method - checking {GameManager.Instance.gardenPlots.Count} plots");
            Debug.Log($"[DROP] Drop world position: {worldPos}");
            
            float closestDistance = maxDropDistance;
            GardenPlot closestPlot = null;
            
            foreach (GardenPlot plot in GameManager.Instance.gardenPlots)
            {
                if (plot == null) continue;
                
                // Try to get position from collider if available (more accurate)
                Vector3 plotWorldPos = plot.transform.position;
                Collider2D plotCollider = plot.GetComponent<Collider2D>();
                if (plotCollider != null)
                {
                    // Use collider's bounds center as position
                    plotWorldPos = plotCollider.bounds.center;
                }
                
                float distance = Vector3.Distance(worldPos, plotWorldPos);
                
                Debug.Log($"[DROP] Plot {plot.name}:");
                Debug.Log($"  - Local position: {plot.transform.localPosition}");
                Debug.Log($"  - World position: {plot.transform.position}");
                if (plotCollider != null)
                {
                    Debug.Log($"  - Collider center: {plotWorldPos}");
                }
                Debug.Log($"  - Distance from drop: {distance:F2}");
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlot = plot;
                }
            }
            
            if (closestPlot != null)
            {
                targetPlot = closestPlot;
                Debug.Log($"[DROP] ✓ Found closest GardenPlot: {targetPlot.name} at distance {closestDistance:F2}");
            }
            else
            {
                Debug.LogWarning($"[DROP] No plot within {maxDropDistance} units of drop point {worldPos}");
                Debug.LogWarning($"[DROP] Try increasing Max Drop Distance in SeedPacket component, or check plot positions");
            }
        }
        else if (targetPlot == null && GameManager.Instance == null)
        {
            Debug.LogError("[DROP] GameManager.Instance is null! Cannot use fallback method.");
        }
        
        // Try to plant the seed
        if (targetPlot != null && plantData != null)
        {
            targetPlot.PlantSeed(plantData);
            wasDroppedOnPlot = true;
            Debug.Log($"✓ Planted {plantData.plantName} on plot {targetPlot.name}!");
        }
        else
        {
            if (targetPlot == null)
            {
                Debug.LogWarning($"No GardenPlot found near drop position: {worldPos}. Make sure garden plots have colliders or are in GameManager's gardenPlots list.");
            }
            if (plantData == null)
            {
                Debug.LogWarning("PlantData is null!");
            }
        }
        
        // Return to original position
        rectTransform.anchoredPosition = originalPosition;
        transform.SetParent(originalParent);
    }
}