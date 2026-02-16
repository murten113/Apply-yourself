using UnityEngine;

/// <summary>
/// Scales the tree based on the current score.
/// Attach to the tree GameObject (or a parent of it).
/// </summary>
public class TreeController : MonoBehaviour
{
    [Header("Scaling")]
    [SerializeField] private float minScale = 0.3f;   // Sapling
    [SerializeField] private float maxScale = 2f;     // Full tree
    [SerializeField] private int scoreForMaxTree = 500;

    [Header("Optional: animate smoothly")]
    [SerializeField] private float scaleLerpSpeed = 2f;

    private Vector3 targetScale;

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    /// <summary>
    /// Called by GardenManager each frame. Updates tree scale from score.
    /// </summary>
    public void UpdateTreeScale(int currentScore)
    {
        float t = Mathf.Clamp01((float)currentScore / scoreForMaxTree);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        targetScale = Vector3.one * scale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleLerpSpeed * Time.deltaTime);
    }
}