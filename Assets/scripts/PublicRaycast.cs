using UnityEngine;

public class PublicRaycast : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera playerCamera;
    
    [Header("Raycast Settings")]
    public float maxDistance = 100f;
    public LayerMask layerMask = -1; // All layers by default
    public bool useMousePosition = true; // If false, uses camera center
    
    [Header("Debug")]
    public bool showDebugRay = true;
    public Color debugRayColor = Color.red;
    public bool showDebugLogs = false; // Toggle debug logs on/off
    
    private RaycastHit hit;
    private bool isHit = false;
    
    void Start()
    {
        // Auto-assign main camera if not set
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("PublicRaycast: No camera assigned and no Main Camera found!");
            }
        }
    }
    
    void Update()
    {
        PerformRaycast();
        
        if (showDebugLogs)
        {
            string tag = GetLookedAtTag();
            if (!string.IsNullOrEmpty(tag))
            {
                Debug.Log($"Looking at: {tag} (Object: {GetLookedAtObject()?.name ?? "null"})");
            }
        }
    }
    
    void PerformRaycast()
    {
        if (playerCamera == null) return;
        
        Ray ray;
        
        if (useMousePosition)
        {
            // Raycast from camera through mouse position
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            // Raycast from camera center forward
            ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        }
        
        // Perform the raycast
        isHit = Physics.Raycast(ray, out hit, maxDistance, layerMask);
        
        // Debug visualization (only visible in Scene view)
        if (showDebugRay)
        {
            Vector3 endPoint = isHit ? hit.point : ray.origin + ray.direction * maxDistance;
            // Draw for longer duration so it's visible
            Debug.DrawLine(ray.origin, endPoint, debugRayColor, 0f, false);
        }
    }
    
    // Public method to get the tag of what the player is looking at
    public string GetLookedAtTag()
    {
        if (isHit && hit.collider != null)
        {
            return hit.collider.tag;
        }
        return ""; // Return empty string if no hit
    }
    
    // Public method to check if raycast is hitting something
    public bool IsLookingAtSomething()
    {
        return isHit;
    }
    
    // Public method to get the GameObject being looked at
    public GameObject GetLookedAtObject()
    {
        if (isHit && hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }
    
    // Public method to get the RaycastHit (for more advanced use)
    public RaycastHit GetHitInfo()
    {
        return hit;
    }
    
    // Public method to get the position where the player is looking
    public Vector3 GetLookedAtPosition()
    {
        if (isHit)
        {
            return hit.point; // Returns the exact hit point
        }
        return Vector3.zero; // Returns zero if nothing is hit
    }
    
    // Public method to get position even if nothing is hit (returns position at max distance)
    public Vector3 GetLookedAtPositionOrMaxDistance()
    {
        if (playerCamera == null) return Vector3.zero;
        
        Ray ray;
        if (useMousePosition)
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        }
        
        if (isHit)
        {
            return hit.point; // Returns the exact hit point
        }
        else
        {
            // Returns position at max distance if nothing is hit
            return ray.origin + ray.direction * maxDistance;
        }
    }
    
    // Public method to check if position is valid (actually hit something)
    public bool HasValidPosition()
    {
        return isHit;
    }
    
    // Public property for easy access
    public string LookedAtTag
    {
        get { return GetLookedAtTag(); }
    }
    
    // Public property to get the hit GameObject
    public GameObject LookedAtObject
    {
        get { return GetLookedAtObject(); }
    }
    
    // Public property for easy access to the hit position
    public Vector3 LookedAtPosition
    {
        get { return GetLookedAtPosition(); }
    }
    
    // Public property to get position even if nothing is hit
    public Vector3 LookedAtPositionOrMaxDistance
    {
        get { return GetLookedAtPositionOrMaxDistance(); }
    }
    
    // OnDrawGizmos - Shows ray in Scene view even when not playing
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebugRay || playerCamera == null) return;
        
        Ray ray;
        if (useMousePosition)
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        }
        
        Vector3 endPoint = isHit ? hit.point : ray.origin + ray.direction * maxDistance;
        Gizmos.color = debugRayColor;
        Gizmos.DrawLine(ray.origin, endPoint);
    }
}