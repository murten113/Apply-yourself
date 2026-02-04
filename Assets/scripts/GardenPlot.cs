using UnityEngine;
using System.Collections.Generic;

public class GardenPlot : MonoBehaviour
{
    [Header("Plot Settings")]
    public bool isUnlocked = true;
    public bool hasDeadPlant = true;
    public Plant currentPlant;
    
    [Header("Available Plants")]
    public List<PlantData> availablePlantTypes = new List<PlantData>();
    
    [Header("Visual")]
    public SpriteRenderer plotRenderer;
    public Sprite emptyPlotSprite;
    public Sprite deadPlantSprite;
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    
    [Header("Plant Prefab")]
    public GameObject plantPrefab; // Generic plant prefab
    
    void Start()
    {
        UpdateVisual();
        
        // Ensure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a Collider2D! Adding BoxCollider2D...");
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f); // Default size
        }
    }
    
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateVisual();
    }
    
    public void RemoveDeadPlant()
    {
        Debug.Log($"{gameObject.name} - RemoveDeadPlant called. Unlocked: {isUnlocked}, HasDeadPlant: {hasDeadPlant}");
        
        if (!isUnlocked)
        {
            Debug.Log("Plot is locked!");
            return;
        }
        if (!hasDeadPlant)
        {
            Debug.Log("No dead plant to remove!");
            return;
        }
        
        hasDeadPlant = false;
        UpdateVisual();
        Debug.Log("Dead plant removed!");
    }
    
    // Updated to use selected plant from GameManager
    public void PlantSeed(PlantData plantData)
    {
        Debug.Log($"{gameObject.name} - PlantSeed called. Unlocked: {isUnlocked}, HasDeadPlant: {hasDeadPlant}, CurrentPlant: {currentPlant != null}, PlantData: {plantData != null}, Prefab: {plantPrefab != null}");
        
        if (!isUnlocked)
        {
            Debug.Log("Plot is locked!");
            return;
        }
        if (hasDeadPlant)
        {
            Debug.Log("Plot has dead plant - remove it first!");
            return;
        }
        if (currentPlant != null)
        {
            Debug.Log("Plot already has a plant!");
            return;
        }
        if (plantData == null)
        {
            Debug.LogWarning("PlantData is null!");
            return;
        }
        if (plantPrefab == null)
        {
            Debug.LogWarning("Plant prefab is null!");
            return;
        }
        
        GameObject plantObj = Instantiate(plantPrefab, transform.position, Quaternion.identity);
        currentPlant = plantObj.GetComponent<Plant>();
        if (currentPlant != null)
        {
            currentPlant.Initialize(plantData);
            Debug.Log($"Successfully planted {plantData.plantName}!");
        }
        else
        {
            Debug.LogError("Plant prefab doesn't have Plant component!");
        }
        
        UpdateVisual();
    }
    
    public void WaterPlant()
    {
        if (!isUnlocked) return;
        if (currentPlant == null) return;
        
        currentPlant.Water();
    }
    
    public void MaintainPlant()
    {
        if (!isUnlocked) return;
        if (currentPlant == null) return;
        
        currentPlant.Maintain();
    }
    
    public bool IsFullyGrown()
    {
        return currentPlant != null && currentPlant.IsFullyGrown();
    }
    
    void UpdateVisual()
    {
        if (plotRenderer == null) return;
        
        if (!isUnlocked)
        {
            plotRenderer.color = lockedColor;
            return;
        }
        
        if (hasDeadPlant)
        {
            if (deadPlantSprite != null)
            {
                plotRenderer.sprite = deadPlantSprite;
            }
        }
        else if (emptyPlotSprite != null)
        {
            plotRenderer.sprite = emptyPlotSprite;
        }
    }
}