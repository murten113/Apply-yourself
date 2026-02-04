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
    public float maxDropDistance = 2f; // Maximum distance to consider a valid drop
    
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
        
        // Convert screen position to world position
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(mainCamera.transform.position.z); // Use camera's z-distance
        
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; // Ensure z is 0 for 2D
        
        Debug.Log($"Drop position - Screen: {screenPos}, World: {worldPos}");
        
        // Try multiple detection methods
        Collider2D hitCollider = null;
        
        // Method 1: OverlapPoint
        hitCollider = Physics2D.OverlapPoint(worldPos);
        
        // Method 2: If OverlapPoint fails, try Raycast
        if (hitCollider == null)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.1f);
            if (hit.collider != null)
            {
                hitCollider = hit.collider;
            }
        }
        
        // Method 3: Try with a larger radius
        if (hitCollider == null)
        {
            hitCollider = Physics2D.OverlapCircle(worldPos, 1f);
        }
        
        // Method 4: Try even larger radius
        if (hitCollider == null)
        {
            hitCollider = Physics2D.OverlapCircle(worldPos, 2f);
        }
        
        GardenPlot targetPlot = null;
        
        if (hitCollider != null)
        {
            targetPlot = hitCollider.GetComponent<GardenPlot>();
            if (targetPlot != null)
            {
                Debug.Log($"Found GardenPlot via collider: {targetPlot.name}");
            }
        }
        
        // Method 5: Fallback - Find closest GardenPlot from GameManager's list
        if (targetPlot == null && GameManager.Instance != null)
        {
            float closestDistance = maxDropDistance;
            GardenPlot closestPlot = null;
            
            foreach (GardenPlot plot in GameManager.Instance.gardenPlots)
            {
                if (plot == null) continue;
                
                float distance = Vector3.Distance(worldPos, plot.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlot = plot;
                }
            }
            
            if (closestPlot != null)
            {
                targetPlot = closestPlot;
                Debug.Log($"Found closest GardenPlot: {targetPlot.name} at distance {closestDistance}");
            }
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