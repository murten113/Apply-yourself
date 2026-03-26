using UnityEngine;

/// <summary>
/// Shows one of three tree models based on player score (GardenManager).
/// Stages: sprout → small tree → final tree at configurable score thresholds.
/// </summary>
public class TreeController : MonoBehaviour
{
    [Header("Score thresholds")]
    [Tooltip("Score needed to show the sprout model")]
    [SerializeField] private int scoreForSprout = 300;
    [Tooltip("Score needed to show the small tree (replaces sprout)")]
    [SerializeField] private int scoreForSmallTree = 600;
    [Tooltip("Score needed to show the final tree")]
    [SerializeField] private int scoreForFinalTree = 900;

    [Header("Models (assign scene children or prefab instances)")]
    [SerializeField] private GameObject sproutModel;
    [SerializeField] private GameObject smallTreeModel;
    [SerializeField] private GameObject finalTreeModel;

    [Header("Before first stage")]
    [Tooltip("If true, all models are hidden when score is below the sprout threshold")]
    [SerializeField] private bool hideAllBelowSproutThreshold = true;

    private int lastAppliedStage = int.MinValue;

    private void Start()
    {
        SyncFromGardenManager();
    }

    private void OnEnable()
    {
        lastAppliedStage = int.MinValue;
        SyncFromGardenManager();
    }

    private void SyncFromGardenManager()
    {
        GardenManager gm = Object.FindFirstObjectByType<GardenManager>();
        if (gm != null)
            UpdateTreeScale(gm.Score);
    }

    /// <summary>
    /// Called by GardenManager each frame. Updates which tree model is visible from score.
    /// </summary>
    public void UpdateTreeScale(int currentScore)
    {
        int stage = GetStageForScore(currentScore);
        if (stage == lastAppliedStage) return;
        ApplyStage(stage, force: false);
    }

    /// <summary>
    /// Stage: -1 = none (or sprout if not hiding), 0 = sprout, 1 = small, 2 = final
    /// </summary>
    private int GetStageForScore(int score)
    {
        if (score < scoreForSprout)
            return hideAllBelowSproutThreshold ? -1 : 0;

        if (score < scoreForSmallTree)
            return 0;
        if (score < scoreForFinalTree)
            return 1;
        return 2;
    }

    private void ApplyStage(int stage, bool force)
    {
        if (!force && stage == lastAppliedStage) return;
        lastAppliedStage = stage;

        if ((sproutModel != null && sproutModel == smallTreeModel) ||
            (sproutModel != null && sproutModel == finalTreeModel) ||
            (smallTreeModel != null && smallTreeModel == finalTreeModel))
        {
            Debug.LogWarning(
                "TreeController: Sprout / Small / Final must be three different GameObjects (usually three child meshes). " +
                "If the same object is assigned to more than one slot, SetActive will fight and nothing will show.",
                this);
        }

        SetActiveSafe(sproutModel, stage == 0);
        SetActiveSafe(smallTreeModel, stage == 1);
        SetActiveSafe(finalTreeModel, stage == 2);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active)
            go.SetActive(active);
    }
}
