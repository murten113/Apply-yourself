using UnityEngine;

[CreateAssetMenu(fileName = "New Plant Data", menuName = "Garden/Plant Data")]
public class PlantData : ScriptableObject
{
    [Header("Plant Info")]
    public string plantName;
    public PlantType plantType;
    public Color plantColor = Color.white;
    
    [Header("Growth Settings")]
    public float growthTime = 10f;
    public float waterTime = 5f;
    public float maintenanceTime = 8f;
    
    [Header("Points")]
    public int pointValue = 10;
    public int maintenanceBonus = 5;
    
}

public enum PlantType
{
    YellowFlower,
    RedFlower,
    PurpleFlower
}