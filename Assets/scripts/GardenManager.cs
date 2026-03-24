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
    [SerializeField] private float minPlantSpacing = 0.6f;   // Minimum XZ distance between planted flowers

    // Runtime state
    private List<Plant> plants = new List<Plant>();
    private int score;
    private float scoreAccumulator;  // Fractional points carried between frames
    private float gameTimer;
    private int unlockedPlotCount;
    private int initiallyUnlockedCount;

    // Public accessors for UI/other systems
    public int Score => score;
    public float GameTimer => gameTimer;
    public PlantType[] AvailablePlantTypes => availablePlantTypes;
    public float GameDuration => gameDurationSeconds;
    public IReadOnlyList<Plant> Plants => plants;

    private void Start()
    {
        gameTimer = gameDurationSeconds;

        // Unlock all plots at start (no unlocking system)
        if (plots != null)
        {
            foreach (var plot in plots)
            {
                if (plot != null && !plot.IsUnlocked)
                {
                    plot.Unlock();
                }
            }
            unlockedPlotCount = plots.Length; // All plots unlocked
        }

        if (FindObjectOfType<GameUI>() == null)
        {
            GameObject uiObj = new GameObject("GameUI");
            uiObj.AddComponent<GameUI>();
        }
    }

    private void Update()
    {
        gameTimer -= Time.deltaTime;
        if (gameTimer <= 0f) gameTimer = 0f;

        UpdateAllPlants();
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
                    scoreAccumulator += p.type.pointIncome * Time.deltaTime;
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

        // Convert accumulated fractional points to whole score (avoids losing points when rounding per-frame)
        int wholePoints = Mathf.FloorToInt(scoreAccumulator);
        score += wholePoints;
        scoreAccumulator -= wholePoints;
    }

    /// <summary>
    /// Remove all dead plants (and plot dead plants) within the given radius of the center point.
    /// Returns the number of dead plants removed.
    /// </summary>
    public int TryRemoveDeadPlantsInArea(Vector3 center, float radius)
    {
        int removed = 0;

        // Remove dead plants from the plants list
        for (int i = plants.Count - 1; i >= 0; i--)
        {
            Plant p = plants[i];
            if (p.stage != PlantStage.Dead) continue;

            float distSq = (p.worldPosition.x - center.x) * (p.worldPosition.x - center.x) +
                          (p.worldPosition.z - center.z) * (p.worldPosition.z - center.z);
            if (distSq <= radius * radius)
            {
                if (p.visualTransform != null)
                    Destroy(p.visualTransform.gameObject);
                plants.RemoveAt(i);
                score += 10;
                removed++;
            }
        }

        // Remove dead plants from plots within radius
        if (plots != null)
        {
            foreach (var plot in plots)
            {
                if (plot == null || !plot.HasDeadPlant) continue;

                Vector3 plotPos = plot.PlantPosition;
                float distSq = (plotPos.x - center.x) * (plotPos.x - center.x) +
                              (plotPos.z - center.z) * (plotPos.z - center.z);
                if (distSq <= radius * radius)
                {
                    plot.RemoveDeadPlant();
                    score += 10;
                    removed++;
                }
            }
        }

        return removed;
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

    /// <summary>
    /// Water all plants within the given radius of the center point.
    /// Returns the number of plants watered.
    /// </summary>
    public int TryWaterPlantsInArea(Vector3 center, float radius)
    {
        int watered = 0;
        foreach (Plant p in plants)
        {
            if (p.stage != PlantStage.NeedsWater && p.stage != PlantStage.Mature)
                continue;

            // Use XZ distance (ignore Y) so plants on flat ground are found correctly
            float distSq = (p.worldPosition.x - center.x) * (p.worldPosition.x - center.x) +
                          (p.worldPosition.z - center.z) * (p.worldPosition.z - center.z);
            if (distSq <= radius * radius && WaterPlant(p))
                watered++;
        }
        return watered;
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
    /// Now allows multiple plants per plot and plants at the exact hit position.
    /// </summary>
    public bool TryPlantSeed(RaycastHit hit, PlantType plantType)
    {
        if (plantType == null) return false;

        // Try collider-based lookup first, then fall back to position-based
        GardenPlot plot = hit.collider.GetComponentInParent<GardenPlot>();
        if (plot == null)
            plot = GetPlotAtPosition(hit.point);

        // Only check if plot exists - remove unlock and dead plant checks
        if (plot == null)
            return false;

        if (!plot.IsUnlocked || !IsPositionInsidePlot(plot, hit.point))
            return false;

        Vector3 plantPosition = hit.point;

        if (IsTooCloseToExistingPlant(plantPosition))
            return false;

        // Ensure plant is slightly above the surface
        plantPosition.y += 0.1f; // Adjust this value based on your needs

        Plant plant = new Plant(plantType, plantPosition);
        CreatePlantVisual(plant, plot);
        plants.Add(plant);
        return true;
    }

    /// <summary>
    /// Plant a seed at a specific world position (uses PublicRaycast position).
    /// Allows multiple plants per plot and plants anywhere on the plot.
    /// </summary>
    public bool TryPlantSeedAtPosition(Vector3 worldPosition, PlantType plantType)
    {
        if (plantType == null) return false;

        // Only allow planting on valid, unlocked plot areas.
        GardenPlot plot = GetPlotAtPosition(worldPosition);
        if (plot == null || !plot.IsUnlocked || !IsPositionInsidePlot(plot, worldPosition))
            return false;

        // Use the exact position provided
        Vector3 plantPosition = worldPosition;
        if (IsTooCloseToExistingPlant(plantPosition))
            return false;

        plantPosition.y += 0.1f; // Slightly above surface

        Plant plant = new Plant(plantType, plantPosition);
        CreatePlantVisual(plant, plot);

        plants.Add(plant);
        return true;
    }

    /// <summary>
    /// Creates plant visual at a specific position (for planting without plots).
    /// Uses model prefab if assigned, otherwise uses cube system.
    /// </summary>
    private void CreatePlantVisualAtPosition(Plant plant, Vector3 position)
    {
        GameObject modelPrefab = GetModelPrefabForStage(plant.type, plant.stage);
        
        if (modelPrefab != null)
        {
            // Use the assigned model
            GameObject model = Instantiate(modelPrefab, position, Quaternion.identity);
            model.name = $"{plant.type.displayName} Plant ({plant.stage})";
            
            // Store initial scale for growth animation
            plant.initialModelScale = model.transform.localScale;
            plant.usesModel = true;
            plant.currentModelStage = plant.stage;
            
            // Start at small size (seed stage)
            float initialHeight = minGrowthHeight;
            model.transform.localScale = plant.initialModelScale * initialHeight;
            
            plant.visualTransform = model.transform;
        }
        else
        {
            // Fallback to cube system
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"{plant.type.displayName} Plant";
            float initialHeight = plantVisualScale * minGrowthHeight;
            cube.transform.localScale = new Vector3(plantVisualScale, initialHeight, plantVisualScale);
            cube.transform.position = position + Vector3.up * (initialHeight * 0.5f);

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = plant.type.flowerColor;
            }

            plant.visualTransform = cube.transform;
            plant.usesModel = false; // Using cube system
        }
    }

    /// <summary>
    /// Creates a plant visual at the plant's position.
    /// Uses model prefab if assigned, otherwise uses cube system.
    /// </summary>
    private void CreatePlantVisual(Plant plant, GardenPlot plot)
    {
        GameObject modelPrefab = GetModelPrefabForStage(plant.type, plant.stage);
        
        if (modelPrefab != null)
        {
            // Use the assigned model for current stage
            GameObject model = Instantiate(modelPrefab, plant.worldPosition, Quaternion.identity);
            model.name = $"{plant.type.displayName} Plant ({plant.stage})";
            
            // Store initial scale for growth animation
            plant.initialModelScale = model.transform.localScale;
            plant.usesModel = true;
            plant.currentModelStage = plant.stage;
            
            // Start at small size (seed stage)
            float initialHeight = minGrowthHeight;
            model.transform.localScale = plant.initialModelScale * initialHeight;
            
            plant.visualTransform = model.transform;
        }
        else
        {
            // Fallback to cube system
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"{plant.type.displayName} Plant";
            float initialHeight = plantVisualScale * minGrowthHeight;
            cube.transform.localScale = new Vector3(plantVisualScale, initialHeight, plantVisualScale);
            cube.transform.position = plant.worldPosition + Vector3.up * (initialHeight * 0.5f);

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = plant.type.flowerColor;
            }

            plant.visualTransform = cube.transform;
            plant.usesModel = false; // Using cube system
        }
    }
    
    /// <summary>
    /// Gets the appropriate model prefab for a given plant stage.
    /// </summary>
    private GameObject GetModelPrefabForStage(PlantType plantType, PlantStage stage)
    {
        if (plantType == null) return null;
        
        // If using single model system (legacy)
        if (plantType.useSingleModel && plantType.plantModelPrefab != null)
        {
            return plantType.plantModelPrefab;
        }
        
        // Stage-based model system
        switch (stage)
        {
            case PlantStage.Seed:
                return plantType.sproutModelPrefab;
                
            case PlantStage.Growing:
                // Growing stage uses sprout (will swap to middle when it reaches 50%)
                return plantType.sproutModelPrefab;
                
            case PlantStage.NeedsWater:
                // Use middle stage model when needs water
                return plantType.middleStageModelPrefab != null ? plantType.middleStageModelPrefab : plantType.sproutModelPrefab;
                
            case PlantStage.Mature:
                // Use mature model when fully grown
                return plantType.matureModelPrefab != null ? plantType.matureModelPrefab : 
                       (plantType.middleStageModelPrefab != null ? plantType.middleStageModelPrefab : plantType.sproutModelPrefab);
                
            case PlantStage.Dead:
                // Dead plants can use mature model (or you could add a dead model)
                return plantType.matureModelPrefab != null ? plantType.matureModelPrefab : 
                       (plantType.middleStageModelPrefab != null ? plantType.middleStageModelPrefab : plantType.sproutModelPrefab);
                
            default:
                return plantType.sproutModelPrefab;
        }
    }
    
    /// <summary>
    /// Swaps the plant model when stage changes.
    /// </summary>
    private void SwapPlantModel(Plant plant)
    {
        if (!plant.usesModel || plant.visualTransform == null) return;
        
        // Determine which model should be used for current stage
        PlantStage targetModelStage;
        GameObject newModelPrefab = null;
        
        // Determine model based on stage and growth progress
        if (plant.stage == PlantStage.Seed || (plant.stage == PlantStage.Growing && plant.growthProgress < 0.5f))
        {
            // Early stages: use sprout
            targetModelStage = PlantStage.Seed;
            newModelPrefab = plant.type.sproutModelPrefab;
        }
        else if (plant.stage == PlantStage.NeedsWater || (plant.stage == PlantStage.Growing && plant.growthProgress >= 0.5f))
        {
            // Middle stage: use middle stage model
            targetModelStage = PlantStage.NeedsWater;
            newModelPrefab = plant.type.middleStageModelPrefab;
            // Fallback to sprout if middle stage model not assigned
            if (newModelPrefab == null)
            {
                newModelPrefab = plant.type.sproutModelPrefab;
            }
        }
        else if (plant.stage == PlantStage.Mature)
        {
            // Mature stage: use mature model
            targetModelStage = PlantStage.Mature;
            newModelPrefab = plant.type.matureModelPrefab;
            // Fallback to middle stage or sprout if mature model not assigned
            if (newModelPrefab == null)
            {
                newModelPrefab = plant.type.middleStageModelPrefab != null ? plant.type.middleStageModelPrefab : plant.type.sproutModelPrefab;
            }
        }
        else
        {
            // Dead or other stages
            targetModelStage = plant.stage;
            newModelPrefab = GetModelPrefabForStage(plant.type, plant.stage);
        }
        
        if (newModelPrefab == null) return;
        
        // Only swap if the model stage has changed
        if (plant.currentModelStage == targetModelStage) return;
        
        // Store position before destroying old model
        Vector3 position = plant.visualTransform.position;
        
        // Destroy old model
        Destroy(plant.visualTransform.gameObject);
        
        // Create new model
        GameObject newModel = Instantiate(newModelPrefab, position, Quaternion.identity);
        newModel.name = $"{plant.type.displayName} Plant ({plant.stage})";
        
        // Preserve scale based on growth
        plant.initialModelScale = newModel.transform.localScale;
        float heightFactor = Mathf.Lerp(minGrowthHeight, 1f, plant.growthProgress);
        newModel.transform.localScale = plant.initialModelScale * heightFactor;
        
        plant.visualTransform = newModel.transform;
        plant.currentModelStage = targetModelStage;
    }

    /// <summary>
    /// Updates the plant visual's color and scale based on stage and growth.
    /// Works with both model prefabs and cube system.
    /// Handles model swapping when stage changes.
    /// </summary>
    private void UpdatePlantVisual(Plant plant)
    {
        if (plant.visualTransform == null) return;

        // Check if we need to swap models (for stage-based model system)
        if (plant.usesModel && !plant.type.useSingleModel)
        {
            SwapPlantModel(plant);
        }

        Transform t = plant.visualTransform;
        if (t == null) return; // Model might have been swapped

        if (plant.usesModel)
        {
            // Handle model prefab scaling
            float heightFactor = Mathf.Lerp(minGrowthHeight, 1f, plant.growthProgress);
            t.localScale = plant.initialModelScale * heightFactor;
            
            // Position model at ground level
            t.position = plant.worldPosition;
            
            // Apply color tint to model (if it has renderers)
            Color displayColor = GetPlantDisplayColor(plant);
            Renderer[] allRenderers = t.GetComponentsInChildren<Renderer>();
            foreach (var r in allRenderers)
            {
                if (r != null && r.material != null)
                {
                    r.material.color = displayColor;
                }
            }
        }
        else
        {
            // Handle cube system (original behavior)
            Renderer renderer = t.GetComponent<Renderer>();
            if (renderer == null) return;

            // Color based on stage
            Color displayColor = GetPlantDisplayColor(plant);
            renderer.material.color = displayColor;

            // Y-axis growth: scale height based on growthProgress (0 to 1)
            float heightFactor = Mathf.Lerp(minGrowthHeight, 1f, plant.growthProgress);
            float baseScale = plantVisualScale;
            float yScale = baseScale * heightFactor;
            t.localScale = new Vector3(baseScale, yScale, baseScale);

            // Keep bottom of cube on the ground
            t.position = plant.worldPosition + Vector3.up * (yScale * 0.5f);
        }
    }

    /// <summary>
    /// Gets the display color for a plant based on its stage and water level.
    /// </summary>
    private Color GetPlantDisplayColor(Plant plant)
    {
        if (plant.stage == PlantStage.Dead)
        {
            return deadPlantColor;
        }
        else if (plant.stage == PlantStage.NeedsWater)
        {
            return Color.Lerp(plant.type.flowerColor, Color.black, needsWaterDarken);
        }
        else if (plant.stage == PlantStage.Mature && plant.waterLevel < 0.5f)
        {
            // Mature plant low on water - darken as water depletes
            float darkenAmount = (1f - plant.waterLevel * 2f) * needsWaterDarken;
            return Color.Lerp(plant.type.flowerColor, Color.black, darkenAmount);
        }
        else
        {
            return plant.type.flowerColor;
        }
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
        if (plots == null || plots.Length == 0) return null;

        GardenPlot nearestPlot = null;
        float nearestDistance = float.MaxValue;
        foreach (var plot in plots)
        {
            if (plot == null) continue;
            float dist = Vector3.Distance(plot.PlantPosition, worldPosition);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestPlot = plot;
            }
        }

        if (nearestPlot != null && IsPositionInsidePlot(nearestPlot, worldPosition))
            return nearestPlot;

        return null;
    }

    private bool IsPositionInsidePlot(GardenPlot plot, Vector3 worldPosition)
    {
        if (plot == null) return false;

        Collider plotCollider = plot.GetComponent<Collider>();
        if (plotCollider != null)
        {
            Vector3 closest = plotCollider.ClosestPoint(worldPosition);
            return (closest - worldPosition).sqrMagnitude <= 0.0004f;
        }

        // Fallback if no collider exists on plot object.
        const float fallbackRadius = 1.5f;
        Vector2 a = new Vector2(plot.PlantPosition.x, plot.PlantPosition.z);
        Vector2 b = new Vector2(worldPosition.x, worldPosition.z);
        return Vector2.SqrMagnitude(a - b) <= fallbackRadius * fallbackRadius;
    }

    private bool IsTooCloseToExistingPlant(Vector3 worldPosition)
    {
        float minDistSq = minPlantSpacing * minPlantSpacing;
        foreach (Plant p in plants)
        {
            float dx = p.worldPosition.x - worldPosition.x;
            float dz = p.worldPosition.z - worldPosition.z;
            if (dx * dx + dz * dz < minDistSq)
                return true;
        }
        return false;
    }
}