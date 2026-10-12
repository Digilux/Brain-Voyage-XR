using UnityEngine;
using UnityEditor;
using System.Text;

public class NegativeScaleScanner
{
    [MenuItem("Tools/Brain/Scan for Negative or Non-Uniform Scale")]
    private static void ScanScene()
    {
        MeshCollider[] allColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);

        StringBuilder report = new StringBuilder();
        int flaggedCount = 0;

        foreach (var col in allColliders)
        {
            Vector3 scale = col.transform.lossyScale; // world-space scale (accounts for parent scaling too)

            bool hasNegative = scale.x < 0 || scale.y < 0 || scale.z < 0;
            bool nonUniform = !Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.y, scale.z);

            if (hasNegative || nonUniform)
            {
                flaggedCount++;
                report.AppendLine(
                    $"{GetPath(col.transform)}  →  lossyScale=({scale.x:0.###}, {scale.y:0.###}, {scale.z:0.###})" +
                    (hasNegative ? "  [NEGATIVE]" : "") +
                    (nonUniform ? "  [NON-UNIFORM]" : ""));
            }
        }

        if (flaggedCount == 0)
        {
            Debug.Log("[NegativeScaleScanner] No negative or non-uniform scaled MeshColliders found.");
        }
        else
        {
            Debug.Log($"[NegativeScaleScanner] Found {flaggedCount} MeshCollider(s) with negative or non-uniform world scale:\n{report}");
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}