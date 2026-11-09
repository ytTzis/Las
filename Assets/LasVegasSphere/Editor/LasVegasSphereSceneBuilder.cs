#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LasVegasSphereSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/LasVegasSphereSync.unity";
    private const string GeneratedFolder = "Assets/LasVegasSphere/Generated";

    [MenuItem("Tools/Las Vegas Sphere/Build Editable Scene")]
    public static void BuildEditableScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = SphereVenueFactory.Build();
        PersistGeneratedAssets(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = GameObject.Find("Audio Visual Sync Experiment");
        EditorUtility.DisplayDialog("Las Vegas Sphere",
            "Editable venue created and saved to:\n" + ScenePath +
            "\n\nPress Play, choose 1–5, then press Space.", "OK");
    }

    private static void PersistGeneratedAssets(GameObject root)
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder("Assets/LasVegasSphere", "Generated");

        var meshes = new Dictionary<Mesh, Mesh>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh source = filter.sharedMesh;
            if (source == null || AssetDatabase.Contains(source)) continue;
            Mesh saved;
            if (!meshes.TryGetValue(source, out saved))
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/" + Sanitize(source.name) + ".asset");
                saved = Object.Instantiate(source);
                saved.name = source.name;
                AssetDatabase.CreateAsset(saved, path);
                meshes.Add(source, saved);
            }
            filter.sharedMesh = saved;
        }

        var materials = new Dictionary<Material, Material>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                Material source = slots[i];
                if (source == null || AssetDatabase.Contains(source)) continue;
                Material saved;
                if (!materials.TryGetValue(source, out saved))
                {
                    string path = AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/" + Sanitize(source.name) + ".mat");
                    saved = Object.Instantiate(source);
                    saved.name = source.name;
                    AssetDatabase.CreateAsset(saved, path);
                    materials.Add(source, saved);
                }
                slots[i] = saved;
            }
            renderer.sharedMaterials = slots;
        }
        AssetDatabase.SaveAssets();
    }

    private static string Sanitize(string value)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value;
    }
}
#endif
