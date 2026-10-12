using UnityEngine;
using UnityEditor;
using System.IO;

public class MeshMirrorTool
{
    [MenuItem("Tools/Brain/Create Mirrored Mesh (Bake X Flip)")]
    private static void CreateMirroredMesh()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Mesh Mirror Tool", "Select a GameObject with a MeshFilter or MeshCollider first.", "OK");
            return;
        }

        Mesh sourceMesh = null;

        MeshFilter mf = selected.GetComponent<MeshFilter>();
        MeshCollider mc = selected.GetComponent<MeshCollider>();

        if (mc != null && mc.sharedMesh != null)
            sourceMesh = mc.sharedMesh;
        else if (mf != null && mf.sharedMesh != null)
            sourceMesh = mf.sharedMesh;

        if (sourceMesh == null)
        {
            EditorUtility.DisplayDialog("Mesh Mirror Tool", "No mesh found on MeshFilter or MeshCollider.", "OK");
            return;
        }

        Mesh mirrored = MirrorMeshX(sourceMesh);
        mirrored.name = sourceMesh.name + "_MirroredX";

        // Save as a new asset next to the original, or in a chosen folder
        string folder = "Assets/MirroredMeshes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "MirroredMeshes");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mirrored.name}.asset");
        AssetDatabase.CreateAsset(mirrored, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MeshMirrorTool] Created mirrored mesh at: {path}");
        EditorUtility.DisplayDialog("Mesh Mirror Tool",
            $"Mirrored mesh created:\n{path}\n\nNow assign it to the Mesh Collider (and Mesh Filter if needed), then set the object's Scale X back to 1.",
            "OK");

        // Ping it in the Project window so it's easy to find/drag
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }

    private static Mesh MirrorMeshX(Mesh source)
    {
        Mesh copy = Object.Instantiate(source);

        // Flip X on all vertices
        Vector3[] verts = copy.vertices;
        for (int i = 0; i < verts.Length; i++)
            verts[i].x = -verts[i].x;
        copy.vertices = verts;

        // Flip X on normals too, so lighting/collision normals stay correct
        Vector3[] normals = copy.normals;
        if (normals != null && normals.Length > 0)
        {
            for (int i = 0; i < normals.Length; i++)
                normals[i].x = -normals[i].x;
            copy.normals = normals;
        }

        // Reverse triangle winding order for every submesh
        // (required after a mirror, or the mesh renders/collides inside-out)
        for (int sub = 0; sub < copy.subMeshCount; sub++)
        {
            int[] tris = copy.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int temp = tris[i];
                tris[i] = tris[i + 1];
                tris[i + 1] = temp;
            }
            copy.SetTriangles(tris, sub);
        }

        copy.RecalculateBounds();
        return copy;
    }
}