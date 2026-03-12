using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tool types the player can use.
/// </summary>
public enum ToolType
{
    Shovel,
    SeedPacket,
    WateringCan
}

/// <summary>
/// Handles tool switching and raycast-based interaction.
/// Uses InputActionReference for New Input System (assign in Inspector).
/// Falls back to legacy Input if actions are not assigned.
/// Works with any movement controller - assign PublicRaycast, GardenManager, and Camera in Inspector.
/// </summary>
public class PlayerTools : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] private ToolType currentTool = ToolType.Shovel;

    [Header("AOE Settings")]
    [Tooltip("Radius for watering can (waters all plants in area)")]
    [SerializeField] private float wateringRadius = 2f;
    [Tooltip("Radius for shovel (removes all dead plants in area)")]
    [SerializeField] private float shovelRadius = 2f;

    [Header("Hold to Repeat (Watering Can & Shovel)")]
    [Tooltip("How often the tool activates while held (seconds). 0.2 = 5x per second")]
    [SerializeField] private float interactRepeatRate = 0.2f;

    [Header("Circle Indicator")]
    [SerializeField] private float circleIndicatorHeight = 0.02f;
    [SerializeField] private Color wateringCircleColor = new Color(0.2f, 0.5f, 1f, 0.6f);
    [SerializeField] private Color shovelCircleColor = new Color(0.6f, 0.3f, 0.1f, 0.6f);
    [SerializeField] private int circleSegments = 32;

    [Header("Input (New Input System) - assign in Inspector")]
    [Tooltip("Button to use the current tool (e.g. Interact)")]
    [SerializeField] private InputActionReference interactAction;
    [Tooltip("Select Shovel")]
    [SerializeField] private InputActionReference selectTool1Action;
    [Tooltip("Select Seed Packet")]
    [SerializeField] private InputActionReference selectTool2Action;
    [Tooltip("Select Watering Can")]
    [SerializeField] private InputActionReference selectTool3Action;
    [Tooltip("Cycle to next seed type (optional, for joystick)")]
    [SerializeField] private InputActionReference selectSeedNextAction;
    [Tooltip("Cycle to previous seed type (optional)")]
    [SerializeField] private InputActionReference selectSeedPrevAction;

    [Header("References")]
    [SerializeField] private PublicRaycast publicRaycast;
    [SerializeField] private GardenManager gardenManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlantType selectedSeedType;

    private int selectedSeedIndex;
    private LineRenderer circleIndicator;
    private GameObject circleIndicatorObj;
    private float interactRepeatTimer;

    public ToolType CurrentTool => currentTool;
    public PlantType SelectedSeedType => selectedSeedType;

    private void OnEnable()
    {
        interactAction?.action?.Enable();
        selectTool1Action?.action?.Enable();
        selectTool2Action?.action?.Enable();
        selectTool3Action?.action?.Enable();
        selectSeedNextAction?.action?.Enable();
        selectSeedPrevAction?.action?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
        selectTool1Action?.action?.Disable();
        selectTool2Action?.action?.Disable();
        selectTool3Action?.action?.Disable();
        selectSeedNextAction?.action?.Disable();
        selectSeedPrevAction?.action?.Disable();
    }

    private void Start()
    {
        SyncSeedSelectionFromManager();
        CreateCircleIndicator();
    }

    private void CreateCircleIndicator()
    {
        circleIndicatorObj = new GameObject("ToolCircleIndicator");
        circleIndicatorObj.transform.SetParent(transform);
        circleIndicator = circleIndicatorObj.AddComponent<LineRenderer>();
        circleIndicator.positionCount = circleSegments + 1;
        circleIndicator.loop = true;
        circleIndicator.useWorldSpace = true;
        circleIndicator.startWidth = 0.05f;
        circleIndicator.endWidth = 0.05f;
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader != null)
            circleIndicator.material = new Material(shader);
        circleIndicatorObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (circleIndicatorObj != null)
            Destroy(circleIndicatorObj);
    }

    private void UpdateCircleIndicator(float radius, Color color)
    {
        if (circleIndicator == null || publicRaycast == null) return;

        Vector3 center = publicRaycast.GetLookedAtPositionOrMaxDistance();
        center.y += circleIndicatorHeight;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            circleIndicator.SetPosition(i, new Vector3(x, center.y, z));
        }
        circleIndicator.startColor = color;
        circleIndicator.endColor = color;
    }

    private void SyncSeedSelectionFromManager()
    {
        if (gardenManager == null || gardenManager.AvailablePlantTypes == null || gardenManager.AvailablePlantTypes.Length == 0)
            return;

        var types = gardenManager.AvailablePlantTypes;
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == selectedSeedType)
            {
                selectedSeedIndex = i;
                return;
            }
        }
        selectedSeedIndex = 0;
        selectedSeedType = types[0];
    }

    private void Update()
    {
        // Tool selection: New Input System or legacy fallback
        if (WasPressed(selectTool1Action)) currentTool = ToolType.Shovel;
        else if (WasPressed(selectTool2Action)) currentTool = ToolType.SeedPacket;
        else if (WasPressed(selectTool3Action)) currentTool = ToolType.WateringCan;
        else
        {
            // Legacy fallback if no InputAction assigned
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentTool = ToolType.Shovel;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentTool = ToolType.SeedPacket;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentTool = ToolType.WateringCan;
        }

        // Seed cycling: New Input System (Next/Prev) or legacy scroll
        if (currentTool == ToolType.SeedPacket && gardenManager != null && gardenManager.AvailablePlantTypes != null && gardenManager.AvailablePlantTypes.Length > 1)
        {
            if (WasPressed(selectSeedNextAction))
            {
                selectedSeedIndex = (selectedSeedIndex + 1) % gardenManager.AvailablePlantTypes.Length;
                selectedSeedType = gardenManager.AvailablePlantTypes[selectedSeedIndex];
            }
            else if (WasPressed(selectSeedPrevAction))
            {
                selectedSeedIndex--;
                if (selectedSeedIndex < 0) selectedSeedIndex = gardenManager.AvailablePlantTypes.Length - 1;
                selectedSeedType = gardenManager.AvailablePlantTypes[selectedSeedIndex];
            }
            else
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll > 0) { CycleSeed(1); }
                else if (scroll < 0) { CycleSeed(-1); }
            }
        }

        // Interact: use tool
        bool interactHeld = IsHeld(interactAction) || Input.GetMouseButton(0);
        bool interactPressed = WasPressed(interactAction) || Input.GetMouseButtonDown(0);

        if (currentTool == ToolType.WateringCan || currentTool == ToolType.Shovel)
        {
            // Hold to repeat for AOE tools
            if (interactHeld)
            {
                interactRepeatTimer -= Time.deltaTime;
                if (interactRepeatTimer <= 0f)
                {
                    TryUseTool();
                    interactRepeatTimer = Mathf.Max(0.05f, interactRepeatRate);
                }
            }
            else
            {
                interactRepeatTimer = 0f;
            }
        }
        else
        {
            // Single press for seed packet
            if (interactPressed)
                TryUseTool();
        }

        // Update circle indicator for AOE tools
        if (circleIndicatorObj != null)
        {
            bool show = (currentTool == ToolType.WateringCan || currentTool == ToolType.Shovel) && publicRaycast != null;
            circleIndicatorObj.SetActive(show);
            if (show)
            {
                float r = currentTool == ToolType.WateringCan ? wateringRadius : shovelRadius;
                Color c = currentTool == ToolType.WateringCan ? wateringCircleColor : shovelCircleColor;
                UpdateCircleIndicator(r, c);
            }
        }
    }

    private bool WasPressed(InputActionReference actionRef)
    {
        return actionRef != null && actionRef.action != null && actionRef.action.WasPressedThisFrame();
    }

    private bool IsHeld(InputActionReference actionRef)
    {
        return actionRef != null && actionRef.action != null && actionRef.action.IsPressed();
    }

    private void CycleSeed(int direction)
    {
        if (gardenManager?.AvailablePlantTypes == null || gardenManager.AvailablePlantTypes.Length <= 1) return;
        selectedSeedIndex = (selectedSeedIndex + direction + gardenManager.AvailablePlantTypes.Length) % gardenManager.AvailablePlantTypes.Length;
        selectedSeedType = gardenManager.AvailablePlantTypes[selectedSeedIndex];
    }

    private void TryUseTool()
    {
        if (gardenManager == null || publicRaycast == null) return;

        Vector3 lookPosition = publicRaycast.GetLookedAtPositionOrMaxDistance();

        switch (currentTool)
        {
            case ToolType.Shovel:
                // AOE shovel: remove all dead plants within radius.
                gardenManager.TryRemoveDeadPlantsInArea(lookPosition, shovelRadius);
                break;

            case ToolType.SeedPacket:
                // Always allow planting - use the position from PublicRaycast
                // This allows planting anywhere, even if not looking at something
                gardenManager.TryPlantSeedAtPosition(lookPosition, selectedSeedType);
                break;

            case ToolType.WateringCan:
                // AOE watering: water all plants within radius.
                gardenManager.TryWaterPlantsInArea(lookPosition, wateringRadius);
                break;
        }
    }
}