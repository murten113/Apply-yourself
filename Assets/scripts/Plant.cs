using UnityEngine;

//<summery>
// Growth stages a plant goes through
//</summery>

public enum PlantStage
{
    Dead, //can be removed with shovel
    Seed, //just planted, growing
    Growing, //Growing from seed to mid-stage
    NeedsWater, //Stuck at 50% until watered
    Mature //Fully grown, generates points, needs maintenance
}


[System.Serializable]
public class Plant
{
    public PlantType type;
    public PlantStage stage;
    public float growthProgress;
    public float waterLevel;
    public Vector3 worldPosition;
    public Transform visualTransform;

    public Plant(PlantType plantType, Vector3 position)
    {
        type = plantType;
        stage = PlantStage.Seed;
        growthProgress = 0f;
        waterLevel = 1f;
        worldPosition = position;
        visualTransform = null;
    }
}