using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Local-only persistence: saves the player garden on quit, keeps a ring of 8 neighbour snapshots,
/// and spawns decorative mature plants under each neighbour plot (e.g. <c>Garden L (1)</c> / <c>Garden R (1)</c>).
/// </summary>
[DefaultExecutionOrder(100)]
public class CommunityGardenPersistence : MonoBehaviour
{
    private const int RingSize = 8;
    private const string FileName = "community_garden.json";
    private const float FallbackCubeScale = 0.35f;

    [Header("References")]
    [SerializeField] private GardenManager gardenManager;
    [Tooltip("Optional. If null, searches for DECORATION/Other Gardens at Start.")]
    [SerializeField] private Transform otherGardensRoot;

    [Header("Debug")]
    [Tooltip("Logs missing plot transforms and spawn counts (Console).")]
    [SerializeField] private bool debugLog;

    private static bool _savedThisSession;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    private void Awake()
    {
        // Allow save again each time the level loads (static survives quit-to-menu).
        _savedThisSession = false;
        if (gardenManager == null)
            gardenManager = FindGardenManager();
    }

    private static GardenManager FindGardenManager()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var gm = root.GetComponentInChildren<GardenManager>(true);
            if (gm != null)
                return gm;
        }
        return null;
    }

    private void Start()
    {
        if (otherGardensRoot == null)
            otherGardensRoot = FindOtherGardensRoot();

        LoadAndApply();
    }

    private static Transform FindOtherGardensRoot()
    {
        var deco = GameObject.Find("DECORATION");
        if (deco == null)
            return null;
        var t = deco.transform.Find("Other Gardens");
        if (t != null)
            return t;
        foreach (Transform c in deco.transform)
        {
            if (c.name.IndexOf("Other", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return c;
        }
        return null;
    }

    private void OnApplicationQuit()
    {
        SaveAll();
    }

    /// <summary>
    /// Call before <see cref="SceneManager.LoadScene"/> (e.g. quit to menu) so the save runs while <see cref="GardenManager"/> still exists.
    /// OnDestroy order is not reliable, so we do not save there.
    /// </summary>
    public static void SaveNow()
    {
        var p = Object.FindFirstObjectByType<CommunityGardenPersistence>();
        if (p != null)
            p.SaveAll();
    }

    private void LoadAndApply()
    {
        var file = ReadOrCreateFile();
        ApplyPlayerGarden(file.playerGarden);
        ApplyNeighbourDecorations(file);
    }

    private static CommunityGardenFile ReadOrCreateFile()
    {
        if (!File.Exists(SavePath))
            return NewEmptyFile();

        try
        {
            string json = File.ReadAllText(SavePath);
            var file = JsonUtility.FromJson<CommunityGardenFile>(json);
            if (file == null)
                return NewEmptyFile();
            NormalizeFile(file);
            return file;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CommunityGarden] Could not read save: {e.Message}");
            return NewEmptyFile();
        }
    }

    private static CommunityGardenFile NewEmptyFile()
    {
        var f = new CommunityGardenFile();
        f.ringSlots = new GardenSnapshot[RingSize];
        for (int i = 0; i < RingSize; i++)
            f.ringSlots[i] = new GardenSnapshot();
        f.playerGarden = new GardenSnapshot();
        f.nextRingWriteIndex = 0;
        return f;
    }

    private static void NormalizeFile(CommunityGardenFile file)
    {
        if (file.playerGarden == null)
            file.playerGarden = new GardenSnapshot();
        if (file.playerGarden.plants == null)
            file.playerGarden.plants = System.Array.Empty<SavedPlant>();

        if (file.ringSlots == null || file.ringSlots.Length != RingSize)
        {
            file.ringSlots = new GardenSnapshot[RingSize];
            for (int i = 0; i < RingSize; i++)
                file.ringSlots[i] = new GardenSnapshot();
        }
        else
        {
            for (int i = 0; i < RingSize; i++)
            {
                if (file.ringSlots[i] == null)
                    file.ringSlots[i] = new GardenSnapshot();
                if (file.ringSlots[i].plants == null)
                    file.ringSlots[i].plants = System.Array.Empty<SavedPlant>();
            }
        }

        file.nextRingWriteIndex = Mathf.Clamp(file.nextRingWriteIndex, 0, RingSize - 1);
    }

    private void ApplyPlayerGarden(GardenSnapshot snapshot)
    {
        if (gardenManager == null || snapshot == null || snapshot.plants == null)
            return;

        gardenManager.ClearRuntimePlantsForPersistence();

        // Plots start with a scene "dead plant" mesh; remove it on any plot we repopulate so we do not stack dead + live visuals.
        var plotsToRevive = new HashSet<int>();
        foreach (var sp in snapshot.plants)
        {
            if (sp.plotIndex >= 0 && sp.plotIndex < gardenManager.PlotCount)
                plotsToRevive.Add(sp.plotIndex);
        }
        foreach (int plotIdx in plotsToRevive)
        {
            GardenPlot plot = gardenManager.GetPlot(plotIdx);
            if (plot != null && plot.HasDeadPlant)
                plot.RemoveDeadPlant();
        }

        foreach (var sp in snapshot.plants)
        {
            var type = ResolvePlantType(sp.displayName);
            if (type == null)
                continue;
            gardenManager.RestorePlantFromSnapshot(type, sp.plotIndex, new Vector3(sp.localX, sp.localY, sp.localZ));
        }
    }

    private void ApplyNeighbourDecorations(CommunityGardenFile file)
    {
        if (otherGardensRoot == null)
        {
            Debug.LogWarning("[CommunityGarden] Other Gardens root not found; neighbour decorations skipped.");
            return;
        }

        int spawned = 0;
        for (int i = 0; i < RingSize; i++)
        {
            var slot = file.ringSlots[i];
            if (slot == null || slot.plants == null || slot.plants.Length == 0)
                continue;

            foreach (var sp in slot.plants)
            {
                var type = ResolvePlantType(sp.displayName);
                if (type == null)
                    continue;

                Transform plotRoot = FindNeighbourPlotTransform(i, sp.plotIndex);
                if (plotRoot == null)
                {
                    if (debugLog)
                    {
                        int n = i + 1;
                        string hint = sp.plotIndex == 0 ? $"Garden L ({n})" : $"Garden R ({n})";
                        Debug.LogWarning($"[CommunityGarden] No plot transform for ring slot {i}, plotIndex {sp.plotIndex}. Expected something like \"{hint}\" under Other Gardens.", this);
                    }
                    continue;
                }

                Vector3 world = plotRoot.TransformPoint(new Vector3(sp.localX, sp.localY, sp.localZ));
                SpawnNeighbourDecoration(type, world, plotRoot);
                spawned++;
            }
        }

        if (debugLog)
            Debug.Log($"[CommunityGarden] Neighbour decorations spawned: {spawned}. (Ring only fills after you leave the level once; first run has no neighbour history.)", this);
    }

    /// <summary>Neighbour index 0–7 maps to garden pairs (1)…(8). plotIndex 0 = left, 1 = right.</summary>
    private Transform FindNeighbourPlotTransform(int neighbourIndex0to7, int plotIndex0or1)
    {
        int n = neighbourIndex0to7 + 1;
        Transform parent = FindNeighbourParent(otherGardensRoot, n);
        string[] names = plotIndex0or1 == 0 ? LeftPlotNameCandidates(n) : RightPlotNameCandidates(n);

        if (parent != null)
        {
            Transform plot = FindFirstPlotMatching(parent, names);
            if (plot != null)
                return plot;
        }

        return FindFirstPlotMatching(otherGardensRoot, names);
    }

    private static string[] LeftPlotNameCandidates(int n)
    {
        return new[]
        {
            $"Garden L ({n})",
            $"gardenL{n}",
            "gardenL"
        };
    }

    private static string[] RightPlotNameCandidates(int n)
    {
        return new[]
        {
            $"Garden R ({n})",
            $"gardenR{n}",
            "gardenR"
        };
    }

    private static Transform FindFirstPlotMatching(Transform root, string[] orderedNames)
    {
        if (root == null)
            return null;
        foreach (var name in orderedNames)
        {
            Transform t = FindDeepChildByExactName(root, name);
            if (t != null)
                return t;
        }
        return null;
    }

    private static Transform FindNeighbourParent(Transform root, int n)
    {
        if (root == null)
            return null;
        string[] candidates =
        {
            $"Neighbour{n}",
            $"Neighbour ({n})",
            $"Neighbour_{n}",
            $"Neighbour {n}"
        };
        foreach (var name in candidates)
        {
            Transform t = FindDeepChildByExactName(root, name);
            if (t != null)
                return t;
        }
        return null;
    }

    private static Transform FindDeepChildByExactName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
                return t;
        }
        return null;
    }

    private void SpawnNeighbourDecoration(PlantType type, Vector3 worldPosition, Transform plotRoot)
    {
        GameObject prefab = type.GetMaturePrefabOrFallback();
        Transform parent = plotRoot;
        GameObject go;

        if (prefab != null)
        {
            go = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
            go.name = $"NeighbourDecor_{type.displayName}";
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"NeighbourDecor_{type.displayName}";
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = worldPosition;
            float s = FallbackCubeScale;
            go.transform.localScale = new Vector3(s, s, s);
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.material.color = type.flowerColor;
        }

        foreach (var col in go.GetComponentsInChildren<Collider>())
            Destroy(col);

        go.transform.SetAsLastSibling();
    }

    private PlantType ResolvePlantType(string displayName)
    {
        if (gardenManager == null || string.IsNullOrEmpty(displayName))
            return null;
        var types = gardenManager.AvailablePlantTypes;
        if (types == null)
            return null;
        foreach (var t in types)
        {
            if (t != null && t.displayName == displayName)
                return t;
        }
        return null;
    }

    private void SaveAll()
    {
        if (_savedThisSession)
            return;
        if (gardenManager == null)
            gardenManager = FindGardenManager();
        if (gardenManager == null)
            return;

        var file = ReadOrCreateFile();
        GardenSnapshot session = CaptureSnapshot(gardenManager);
        file.playerGarden = session;

        if (file.ringSlots == null || file.ringSlots.Length != RingSize)
            NormalizeFile(file);

        int idx = Mathf.Clamp(file.nextRingWriteIndex, 0, RingSize - 1);
        file.ringSlots[idx] = CloneSnapshot(session);
        file.nextRingWriteIndex = (idx + 1) % RingSize;

        try
        {
            string json = JsonUtility.ToJson(file, prettyPrint: false);
            File.WriteAllText(SavePath, json);
            _savedThisSession = true;
            // State is on disk; tear down runtime plants so nothing survives into teardown/reload (fixes duplicate visuals / double scoring).
            gardenManager.ClearRuntimePlantsForPersistence();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CommunityGarden] Save failed: {e.Message}");
        }
    }

    private static GardenSnapshot CloneSnapshot(GardenSnapshot src)
    {
        if (src == null || src.plants == null)
            return new GardenSnapshot { plants = System.Array.Empty<SavedPlant>() };

        var copy = new SavedPlant[src.plants.Length];
        for (int i = 0; i < src.plants.Length; i++)
        {
            var p = src.plants[i];
            copy[i] = new SavedPlant
            {
                displayName = p.displayName,
                plotIndex = p.plotIndex,
                localX = p.localX,
                localY = p.localY,
                localZ = p.localZ
            };
        }
        return new GardenSnapshot { plants = copy };
    }

    private GardenSnapshot CaptureSnapshot(GardenManager gm)
    {
        var list = new System.Collections.Generic.List<SavedPlant>();
        int plotCount = gm.PlotCount;

        foreach (var p in gm.Plants)
        {
            if (p == null || p.type == null || p.stage == PlantStage.Dead)
                continue;

            int plotIdx = p.ownerPlotIndex;
            if (plotIdx < 0 || plotIdx >= plotCount)
                plotIdx = FindClosestPlotIndex(gm, p.worldPosition, plotCount);
            if (plotIdx < 0 || plotIdx >= plotCount)
                continue;

            GardenPlot plot = gm.GetPlot(plotIdx);
            if (plot == null)
                continue;

            Vector3 local = plot.transform.InverseTransformPoint(p.worldPosition);
            list.Add(new SavedPlant
            {
                displayName = p.type.displayName,
                plotIndex = plotIdx,
                localX = local.x,
                localY = local.y,
                localZ = local.z
            });
        }

        return new GardenSnapshot { plants = list.ToArray() };
    }

    private static int FindClosestPlotIndex(GardenManager gm, Vector3 worldPosition, int plotCount)
    {
        float best = float.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < plotCount; i++)
        {
            var plot = gm.GetPlot(i);
            if (plot == null)
                continue;
            float d = Vector3.SqrMagnitude(plot.transform.position - worldPosition);
            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }
        return bestIdx;
    }
}
