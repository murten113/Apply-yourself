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
    /// <summary>Set when planted on a plot; used for save/load (avoids wrong plot from distance checks).</summary>
    public int ownerPlotIndex = -1;
    public Transform visualTransform;
    public Vector3 initialModelScale; // Stores the original scale of the model (for growth animation)
    public bool usesModel; // True if using a model prefab, false if using cube system
    public PlantStage currentModelStage; // Tracks which stage model is currently displayed

    public Plant(PlantType plantType, Vector3 position)
    {
        type = plantType;
        stage = PlantStage.Seed;
        growthProgress = 0f;
        waterLevel = 1f;
        worldPosition = position;
        visualTransform = null;
        initialModelScale = Vector3.one;
        usesModel = false;
        currentModelStage = PlantStage.Seed; // Track which model stage we're showing
    }
}