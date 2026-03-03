using UnityEngine;

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
/// Attach to the same GameObject as FirstPersonController.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerTools : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] private ToolType currentTool = ToolType.Shovel;
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactLayer; // Optional: limit what we can hit

    [Header("References")]
    [SerializeField] private PublicRaycast publicRaycast; // for looking at plants
    [SerializeField] private GardenManager gardenManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlantType selectedSeedType; // Fallback if no GardenManager; otherwise synced from AvailablePlantTypes

    private int selectedSeedIndex;

    public ToolType CurrentTool => currentTool;
    public PlantType SelectedSeedType => selectedSeedType;

    private void Start()
    {
        SyncSeedSelectionFromManager();
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
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentTool = ToolType.Shovel;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentTool = ToolType.SeedPacket;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentTool = ToolType.WateringCan;

        if (currentTool == ToolType.SeedPacket && gardenManager != null && gardenManager.AvailablePlantTypes != null && gardenManager.AvailablePlantTypes.Length > 1)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0)
            {
                selectedSeedIndex = (selectedSeedIndex + 1) % gardenManager.AvailablePlantTypes.Length;
                selectedSeedType = gardenManager.AvailablePlantTypes[selectedSeedIndex];
            }
            else if (scroll < 0)
            {
                selectedSeedIndex--;
                if (selectedSeedIndex < 0) selectedSeedIndex = gardenManager.AvailablePlantTypes.Length - 1;
                selectedSeedType = gardenManager.AvailablePlantTypes[selectedSeedIndex];
            }
        }

        if (Input.GetMouseButtonDown(0))
            TryUseTool();
    }

    private void TryUseTool()
    {
        if (gardenManager == null || publicRaycast == null) return;

        // Use PublicRaycast instead of doing our own raycast
        bool isLookingAtSomething = publicRaycast.IsLookingAtSomething();
        Vector3 lookPosition = publicRaycast.GetLookedAtPositionOrMaxDistance();

        switch (currentTool)
        {
            case ToolType.Shovel:
                if (isLookingAtSomething)
                {
                    RaycastHit hit = publicRaycast.GetHitInfo();
                    // Only use hit if it's valid (has a collider)
                    if (hit.collider != null)
                    {
                        gardenManager.TryRemoveDeadPlant(hit);
                    }
                }
                break;

            case ToolType.SeedPacket:
                // Always allow planting - use the position from PublicRaycast
                // This allows planting anywhere, even if not looking at something
                gardenManager.TryPlantSeedAtPosition(lookPosition, selectedSeedType);
                break;

            case ToolType.WateringCan:
                // Watering can work with position
                gardenManager.TryWaterPlantAtPoint(lookPosition);
                break;
        }
    }
}