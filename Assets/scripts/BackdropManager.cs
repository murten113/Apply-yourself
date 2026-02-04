using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BackdropState
{
    public int scoreThreshold;
    public Sprite backdropSprite;
    public string description; // For debugging/editor
}

public class BackdropManager : MonoBehaviour
{
    public static BackdropManager Instance { get; private set; }
    
    [Header("Backdrop Settings")]
    public SpriteRenderer backdropRenderer; // If using SpriteRenderer
    public UnityEngine.UI.Image backdropImage; // If using UI Image (alternative)
    
    [Header("Backdrop States")]
    public List<BackdropState> backdropStates = new List<BackdropState>();
    
    private int currentBackdropIndex = -1;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Set initial backdrop (lowest threshold or first state)
        if (backdropStates.Count > 0)
        {
            SetBackdrop(0);
        }
    }
    
    public void CheckScoreThresholds(int currentScore)
    {
        // Find the highest threshold we've reached
        int highestReachedIndex = -1;
        
        for (int i = 0; i < backdropStates.Count; i++)
        {
            if (currentScore >= backdropStates[i].scoreThreshold)
            {
                highestReachedIndex = i;
            }
            else
            {
                break; // List should be sorted by threshold
            }
        }
        
        // Only change backdrop if we've reached a new threshold
        if (highestReachedIndex > currentBackdropIndex)
        {
            SetBackdrop(highestReachedIndex);
        }
    }
    
    void SetBackdrop(int index)
    {
        if (index < 0 || index >= backdropStates.Count) return;
        if (backdropStates[index].backdropSprite == null) return;
        
        currentBackdropIndex = index;
        
        // Update SpriteRenderer if using that
        if (backdropRenderer != null)
        {
            backdropRenderer.sprite = backdropStates[index].backdropSprite;
        }
        
        // Update UI Image if using that
        if (backdropImage != null)
        {
            backdropImage.sprite = backdropStates[index].backdropSprite;
        }
        
        Debug.Log($"Backdrop changed to state {index} at score threshold {backdropStates[index].scoreThreshold}");
    }
}