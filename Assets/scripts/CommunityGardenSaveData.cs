using System;
using UnityEngine;

/// <summary>
/// Serializable payload for local community garden save (JsonUtility).
/// </summary>
[Serializable]
public class SavedPlant
{
    public string displayName;
    /// <summary>0 = first player plot, 1 = second.</summary>
    public int plotIndex;
    public float localX;
    public float localY;
    public float localZ;
}

/// <summary>
/// One garden layout (player or one neighbour slot).
/// </summary>
[Serializable]
public class GardenSnapshot
{
    public SavedPlant[] plants = Array.Empty<SavedPlant>();
}

/// <summary>
/// Root JSON object for <see cref="CommunityGardenPersistence"/>.
/// </summary>
[Serializable]
public class CommunityGardenFile
{
    public int version = 1;
    /// <summary>Next ring index to write (0–7). Advances every quit, including empty snapshots.</summary>
    public int nextRingWriteIndex;
    /// <summary>Last saved player garden for resume.</summary>
    public GardenSnapshot playerGarden = new GardenSnapshot();
    /// <summary>Fixed 8 slots; unused slots may have empty plant arrays.</summary>
    public GardenSnapshot[] ringSlots = new GardenSnapshot[8];
}
