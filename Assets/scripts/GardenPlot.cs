using UnityEngine;

/// <summary>
/// Represents one plot in the garden. Can be empty, have a dead plant, or a living plant.
/// Attach to a GameObject (e.g. a plane or quad) that marks the plot position.
/// </summary>
public class GardenPlot : MonoBehaviour
{
    [Header("Plot State")]
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private bool hasDeadPlant = true;  // Start with dead plants to remove

    [Header("Dead Plant Visual")]
    [SerializeField] private float deadPlantScale = 0.25f;
    [SerializeField] private Color deadPlantColor = new Color(0.4f, 0.26f, 0.13f);

    private GameObject deadPlantVisual;

    /// <summary>
    /// Whether the player can plant or interact here.
    /// </summary>
    public bool IsUnlocked => isUnlocked;

    /// <summary>
    /// Whether there's a dead plant that can be shoveled.
    /// </summary>
    public bool HasDeadPlant => hasDeadPlant;

    /// <summary>
    /// World position where a new plant should be placed (on top of the plot surface).
    /// Uses the plot's top face so plants sit visibly above the ground.
    /// </summary>
    public Vector3 PlantPosition => transform.position + transform.up * (transform.lossyScale.y * 0.5f + 0.05f);

    private void Start()
    {
        if (hasDeadPlant)
            CreateDeadPlantVisual();
    }

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void RemoveDeadPlant()
    {
        hasDeadPlant = false;
        if (deadPlantVisual != null)
        {
            Destroy(deadPlantVisual);
            deadPlantVisual = null;
        }
    }

    public void SetDeadPlant(bool value)
    {
        hasDeadPlant = value;
    }

    private void CreateDeadPlantVisual()
    {
        if (deadPlantVisual != null) return;

        deadPlantVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deadPlantVisual.name = "Dead Plant";
        deadPlantVisual.transform.position = PlantPosition + Vector3.up * (deadPlantScale * 0.5f);
        deadPlantVisual.transform.localScale = Vector3.one * deadPlantScale;

        Renderer r = deadPlantVisual.GetComponent<Renderer>();
        if (r != null)
            r.material.color = deadPlantColor;
    }
}