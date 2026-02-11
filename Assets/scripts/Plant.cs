using UnityEngine;

//<summery>
// Growth stages a plant goes through
//</summery>

public enum PlantStage
{
    Dead, //can be removed with shovel
    Seed. //just plantes, growing
    Growing, //Growing from seed to mid-stage
    NeedsWater, //Stuck at 50% until watered
    Mature //Fully grown, generates points, needs maintenance
}
