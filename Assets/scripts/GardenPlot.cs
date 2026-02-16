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

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void RemoveDeadPlant()
    {
        hasDeadPlant = false;
    }

    public void SetDeadPlant(bool value)
    {
        hasDeadPlant = value;
    }
}