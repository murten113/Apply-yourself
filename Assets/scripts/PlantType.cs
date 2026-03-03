using UnityEngine;


//<summery>
// ScriptableObject that defines stats for a plant type
//create via right-click in project window -> create -> plant type
//</summery>
[CreateAssetMenu(fileName = "NewPlantType", menuName = "Garden/Plant Type")]
public class PlantType : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Flower";
    public Color flowerColor = Color.yellow;
    
    [Header("Visual Models (Optional)")]
    [Tooltip("Optional: Assign 3D model/prefabs for different plant stages. If not assigned, uses colored cube system.")]
    public GameObject sproutModelPrefab; // Seed/Growing stage (0-50% growth)
    public GameObject middleStageModelPrefab; // NeedsWater/Growing stage (50% growth, needs first water)
    public GameObject matureModelPrefab; // Mature stage (fully grown)
    
    [Tooltip("If true, uses the single model system (legacy). If false, uses stage-based models.")]
    public bool useSingleModel = false;
    [Tooltip("Legacy: Single model for all stages (only used if useSingleModel is true).")]
    public GameObject plantModelPrefab;

    [Header("Growth")]
    [Tooltip("How fast the plant grows form seed to mid-stage (0.5). 1 = normal, 1.5 = fast, 0.7 = slow")]
    public float growthSpeed = 1f;

    [Header("Income & Maintenance")]
    [Tooltip("Points per second when the plant is fully mature")]
    public float pointIncome = 5f;
    [Tooltip("How fast water depletes when mature. Higher = needs watering more often")]
    public float maintenanceRate = 0.1f;
    [Tooltip("Max water level. Higher = longer between waterings")]
    public float waterCapacity = 1f;
}
