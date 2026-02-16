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
    [SerializeField] private GardenManager gardenManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlantType selectedSeedType; // Which seed to plant when using SeedPacket

    public ToolType CurrentTool => currentTool;

    private void Update()
    { 
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentTool = ToolType.Shovel;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentTool = ToolType.SeedPacket;
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentTool = ToolType.WateringCan;

        if (Input.GetMouseButtonDown(0))
            TryUseTool();
    }

    private void TryUseTool()
    {
        if (gardenManager == null || playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange);

        switch (currentTool)
        {
            case ToolType.WateringCan:
                // Watering uses "look at" - finds plant in view direction, ignores raycast hits
                if (gardenManager.TryWaterPlantLookingAt(ray, interactRange))
                    return;
                break;
        }

        foreach (RaycastHit hit in hits)
        {
            bool success = false;
            switch (currentTool)
            {
                case ToolType.Shovel:
                    success = gardenManager.TryRemoveDeadPlant(hit);
                    break;
                case ToolType.SeedPacket:
                    success = gardenManager.TryPlantSeed(hit, selectedSeedType);
                    break;
                case ToolType.WateringCan:
                    success = gardenManager.TryWaterPlantAtPoint(hit.point);
                    break;
            }
            if (success) return;
        }

        if (currentTool == ToolType.WateringCan)
        {
            Vector3 fallbackPoint = hits.Length > 0 ? hits[0].point : ray.origin + ray.direction * (interactRange * 0.5f);
            gardenManager.TryWaterPlantAtPoint(fallbackPoint);
        }
    }
}