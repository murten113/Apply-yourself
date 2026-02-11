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
