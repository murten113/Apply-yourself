using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central game manager. Holds the plant list, score, timer, and drives growth logic.
/// </summary>
public class GardenManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float gameDurationSeconds = 180f;
    [SerializeField] private int maturePlantsNeededToUnlockPlot = 3;

    [Header("References")]
    [SerializeField] private PlantType[] availablePlantTypes;
    [SerializeField] private GardenPlot[] plots;
    [SerializeField] private TreeController treeController;

    [Header("Plant Visuals")]
    [SerializeField] private float plantVisualScale = 0.3f;
    [SerializeField] private bool debugWatering = false;  // Enable in Inspector to trace watering issues
    [SerializeField] private float minGrowthHeight = 0.3f;  // Y scale multiplier at 0% growth (0.3 = 30% height)
    [SerializeField] private Color deadPlantColor = new Color(0.4f, 0.26f, 0.13f);  // Brown
    [SerializeField] private float needsWaterDarken = 0.5f;  // How much to darken when needing water (0.5 = 50% darker)

    // Runtime state
    private List<Plant> plants = new List<Plant>();
    private int score;
    private float gameTimer;
    private int unlockedPlotCount = 1; // Start with 1 plot available

    // Public accessors for UI/other systems
    public int Score => score;
    public float GameTimer => gameTimer;
    public float GameDuration => gameDurationSeconds;
    public IReadOnlyList<Plant> Plants => plants;

    private void Start()
    {
        gameTimer = gameDurationSeconds;
    }

    private void Update()
    {
        gameTimer -= Time.deltaTime;
        if (gameTimer <= 0f) gameTimer = 0f;

        UpdateAllPlants();
        CheckPlotUnlock();
        if (treeController != null)
            treeController.UpdateTreeScale(score);
    }

    /// <summary>
    /// Iterates over all plants and updates growth, water, and points.
    /// </summary>
    private void UpdateAllPlants()
    {
        for (int i = plants.Count - 1; i >= 0; i--)
        {
            Plant p = plants[i];

            switch (p.stage)
            {
                case PlantStage.Seed:
                case PlantStage.Growing:
                    float prevProgress = p.growthProgress;
                    p.growthProgress += p.type.growthSpeed * Time.deltaTime * 0.1f;
                    // Only transition to NeedsWater when FIRST crossing 0.5 (not when already past it after watering)
                    if (prevProgress < 0.5f && p.growthProgress >= 0.5f)
                    {
                        p.stage = PlantStage.NeedsWater;
                        p.growthProgress = 0.5f;
                    }
                    else if (p.growthProgress >= 1f)
                    {
                        p.stage = PlantStage.Mature;
                        p.growthProgress = 1f;
                        p.waterLevel = 1f;
                    }
                    break;

                case PlantStage.NeedsWater:
                    // Stuck until watered by player
                    break;

                case PlantStage.Mature:
                    score += Mathf.RoundToInt(p.type.pointIncome * Time.deltaTime);
                    p.waterLevel -= p.type.maintenanceRate * Time.deltaTime;
                    if (p.waterLevel <= 0f)
                    {
                        p.stage = PlantStage.Dead;
                        p.waterLevel = 0f;
                    }
                    break;

                case PlantStage.Dead:
                    // Stays in list until shoveled
                    break;
            }
            UpdatePlantVisual(p);
        }
    }

    private void CheckPlotUnlock()
    {
        int matureCount = plants.Count(p => p.stage == PlantStage.Mature);
        int required = maturePlantsNeededToUnlockPlot * unlockedPlotCount;
        if (matureCount >= required && unlockedPlotCount < plots.Length)
        {
            unlockedPlotCount++;
            plots[unlockedPlotCount - 1].Unlock();
        }
    }

    /// <summary>
    /// Try to remove a dead plant. Uses raycast hit for reliable plot/plant detection.
    /// </summary>
    public bool TryRemoveDeadPlant(RaycastHit hit)
    {
        GardenPlot plot = hit.collider.GetComponentInParent<GardenPlot>();
        if (plot != null && plot.HasDeadPlant)
        {
            plot.RemoveDeadPlant();
            score += 10;
            return true;
        }

        Plant found = FindPlantByHit(hit);
        if (found != null && found.stage == PlantStage.Dead)
        {
            if (found.visualTransform != null)
                Destroy(found.visualTransform.gameObject);
            plants.Remove(found);
            score += 10;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to water a plant. Uses the SAME plot-finding logic as shovel/seed (which work).
    /// </summary>
    public bool TryWaterPlant(RaycastHit hit)
    {
        // Same approach as TryPlantSeed and TryRemoveDeadPlant: get plot from hit first
        GardenPlot plot = hit.collider.GetComponentInParent<GardenPlot>();
        if (plot == null)
            plot = GetPlotAtPosition(hit.point);

        if (plot == null) return false;

        Plant found = FindPlantInPlot(plot);
        return found != null && WaterPlant(found);
    }

    /// <summary>
    /// Try to water the plant the player is looking at (ray from camera).
    /// Works regardless of what the raycast hits - finds the plant closest to the look direction.
    /// </summary>
    public bool TryWaterPlantLookingAt(Ray ray, float maxRange)
    {
        Plant best = null;
        float bestDistToRay = float.MaxValue;

        foreach (Plant p in plants)
        {
            if (p.stage != PlantStage.NeedsWater && p.stage != PlantStage.Mature)
                continue;

            float distToCamera = Vector3.Distance(ray.origin, p.worldPosition);
            if (distToCamera > maxRange) continue;

            // Distance from plant to ray (point-to-line)
            Vector3 toPlant = p.worldPosition - ray.origin;
            float alongRay = Vector3.Dot(toPlant, ray.direction);
            if (alongRay < 0) continue;  // Plant is behind us

            Vector3 closestOnRay = ray.origin + ray.direction * alongRay;
            float distToRay = Vector3.Distance(p.worldPosition, closestOnRay);
            if (distToRay < bestDistToRay && distToRay < 2f)  // Within 2 units of look direction
            {
                bestDistToRay = distToRay;
                best = p;
            }
        }
        return best != null && WaterPlant(best);
    }

    /// <summary>
    /// Try to water a plant near the given world position.
    /// </summary>
    public bool TryWaterPlantAtPoint(Vector3 worldPoint)
    {
        Plant found = FindPlantNearPosition(worldPoint);
        if (debugWatering)
        {
            Debug.Log($"[Water] Point:{worldPoint} Found:{(found != null)} Stage:{(found != null ? found.stage.ToString() : "N/A")} PlantsCount:{plants.Count}");
            if (found == null && plants.Count > 0)
            {
                var nearest = plants.OrderBy(p => Vector3.Distance(p.worldPosition, worldPoint)).First();
                Debug.Log($"[Water] Nearest plant at {nearest.worldPosition} dist={Vector3.Distance(nearest.worldPosition, worldPoint):F2}");
            }
        }
        return found != null && WaterPlant(found);
    }

    private bool WaterPlant(Plant plant)
    {
        if (plant.stage == PlantStage.NeedsWater)
        {
            plant.stage = PlantStage.Growing;
            plant.growthProgress = 0.5f;
            if (debugWatering) Debug.Log("[Water] Success: NeedsWater -> Growing");
            return true;
        }
        if (plant.stage == PlantStage.Mature)
        {
            plant.waterLevel = Mathf.Min(1f, plant.waterLevel + 0.5f);
            if (debugWatering) Debug.Log("[Water] Success: Refilled mature plant");
            return true;
        }
        if (debugWatering) Debug.Log($"[Water] Plant in wrong stage: {plant.stage}");
        return false;
    }

    private Plant FindPlantNearPosition(Vector3 worldPoint)
    {
        const float range = 4f;  // Generous range - hit point may be on player or edge of plot
        return plants
            .Where(p => Vector3.Distance(p.worldPosition, worldPoint) <= range)
            .OrderBy(p => Vector3.Distance(p.worldPosition, worldPoint))
            .FirstOrDefault();
    }

    private Plant FindPlantInPlot(GardenPlot plot)
    {
        const float range = 2.5f;
        return plants
            .Where(p => Vector3.Distance(p.worldPosition, plot.PlantPosition) <= range)
            .OrderBy(p => Vector3.Distance(p.worldPosition, plot.PlantPosition))
            .FirstOrDefault();
    }

    /// <summary>
    /// Try to plant a new seed. Uses raycast hit for reliable plot detection.
    /// </summary>
    public bool TryPlantSeed(RaycastHit hit, PlantType plantType)
    {
        if (plantType == null) return false;

        // Try collider-based lookup first, then fall back to position-based
        // (handles cases where the hit collider isn't on the plot hierarchy, e.g. shared ground plane)
        GardenPlot plot = hit.collider.GetComponentInParent<GardenPlot>();
        if (plot == null)
            plot = GetPlotAtPosition(hit.point);

        if (plot == null || !plot.IsUnlocked || plot.HasDeadPlant)
            return false;

        // Prevent multiple plants per plot
        if (FindPlantInPlot(plot) != null)
            return false;

        Vector3 plantPosition = plot.PlantPosition;
        Plant plant = new Plant(plantType, plantPosition);
        CreatePlantVisual(plant, plot);
        plants.Add(plant);
        return true;
    }

    /// <summary>
    /// Creates a simple colored cube at the plant's position as visual feedback.
    /// Parents under the plot so GetComponentInParent finds the plot when clicking the plant.
    /// </summary>
    private void CreatePlantVisual(Plant plant, GardenPlot plot)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"{plant.type.displayName} Plant";
        float initialHeight = plantVisualScale * minGrowthHeight;
        cube.transform.localScale = new Vector3(plantVisualScale, initialHeight, plantVisualScale);
        cube.transform.position = plant.worldPosition + Vector3.up * (initialHeight * 0.5f);

        // Don't parent under plot - plot has scale (2,0.5,2) which would flatten the plant

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = plant.type.flowerColor;
        }

        plant.visualTransform = cube.transform;
    }

    /// <summary>
    /// Updates the plant cube's color and scale based on stage and growth.
    /// </summary>
    private void UpdatePlantVisual(Plant plant)
    {
        if (plant.visualTransform == null) return;

        Transform t = plant.visualTransform;
        Renderer renderer = t.GetComponent<Renderer>();
        if (renderer == null) return;

        // Color based on stage
        Color displayColor;
        if (plant.stage == PlantStage.Dead)
        {
            displayColor = deadPlantColor;
        }
        else if (plant.stage == PlantStage.NeedsWater)
        {
            displayColor = Color.Lerp(plant.type.flowerColor, Color.black, needsWaterDarken);
        }
        else if (plant.stage == PlantStage.Mature && plant.waterLevel < 0.5f)
        {
            // Mature plant low on water - darken as water depletes (0.5 = full color, 0 = darkest)
            float darkenAmount = (1f - plant.waterLevel * 2f) * needsWaterDarken;  // 0-0.5 water -> 0 to 0.5 darken
            displayColor = Color.Lerp(plant.type.flowerColor, Color.black, darkenAmount);
        }
        else
        {
            displayColor = plant.type.flowerColor;
        }
        renderer.material.color = displayColor;

        // Y-axis growth: scale height based on growthProgress (0 to 1)
        float heightFactor = Mathf.Lerp(minGrowthHeight, 1f, plant.growthProgress);
        float baseScale = plantVisualScale;
        float yScale = baseScale * heightFactor;
        t.localScale = new Vector3(baseScale, yScale, baseScale);

        // Keep bottom of cube on the ground
        t.position = plant.worldPosition + Vector3.up * (yScale * 0.5f);
    }

    /// <summary>
    /// Finds a plant from a raycast hit. Tries multiple methods for reliable detection:
    /// 1. If we hit a plant cube (our visual), find that plant directly
    /// 2. If we hit a collider with GardenPlot, find the plant in that plot
    /// 3. Fallback: find nearest plant to hit point (handles shared ground planes, etc.)
    /// </summary>
    private Plant FindPlantByHit(RaycastHit hit)
    {
        // Case 1: We hit a plant cube - find the plant that owns this visual
        if (hit.transform != null)
        {
            foreach (Plant p in plants)
            {
                if (p.visualTransform != null && p.visualTransform == hit.transform)
                    return p;
            }
        }

        // Case 2: We hit a plot (collider has GardenPlot in hierarchy)
        if (hit.collider != null)
        {
            GardenPlot plot = hit.collider.GetComponentInParent<GardenPlot>();
            if (plot != null)
            {
                const float plotRange = 2f;
                return plants
                    .Where(p => Vector3.Distance(p.worldPosition, plot.PlantPosition) <= plotRange)
                    .OrderBy(p => Vector3.Distance(p.worldPosition, plot.PlantPosition))
                    .FirstOrDefault();
            }
        }

        // Case 3: Fallback - find nearest plant to hit point (handles shared ground, etc.)
        const float fallbackRange = 2.5f;
        return plants
            .Where(p => Vector3.Distance(p.worldPosition, hit.point) <= fallbackRange)
            .OrderBy(p => Vector3.Distance(p.worldPosition, hit.point))
            .FirstOrDefault();
    }

    private GardenPlot GetPlotAtPosition(Vector3 worldPosition)
    {
        float range = 2.5f;  // Generous range for plots with scale 2x2
        foreach (var plot in plots)
        {
            if (Vector3.Distance(plot.PlantPosition, worldPosition) <= range)
                return plot;
        }
        return null;
    }
}