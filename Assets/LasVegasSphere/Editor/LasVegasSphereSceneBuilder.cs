#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class LasVegasSphereSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/LasVegasSphereSync.unity";
    private const string GeneratedFolder = "Assets/LasVegasSphere/Generated";

    [MenuItem("Tools/Las Vegas Sphere/Build Editable Scene")]
    public static void BuildEditableScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = SphereVenueFactory.Build();
        AssignExperimentVideos(root);

        // Generated assets are fully reproducible. Replacing this folder keeps
        // repeated builds from accumulating duplicate materials and meshes.
        if (AssetDatabase.IsValidFolder(GeneratedFolder)) AssetDatabase.DeleteAsset(GeneratedFolder);
        AssetDatabase.CreateFolder("Assets/LasVegasSphere", "Generated");
        PersistGeneratedAssets(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = GameObject.Find("Experiment System");
        EditorUtility.DisplayDialog("Las Vegas Sphere",
            "Editable venue created and saved to:\n" + ScenePath +
            "\n\nPress Play. Use Q/W/E for scenes, 1-5 for offsets, and Space to play.", "OK");
    }

    private static void AssignExperimentVideos(GameObject root)
    {
        SphericalVideoScreen screen = root.GetComponentInChildren<SphericalVideoScreen>(true);
        if (screen == null) return;
        screen.ConfigureExperimentVideos(
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Clap.mp4"),
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Explosion.mp4"),
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Aircraft.mp4"));
    }

    private static void PersistGeneratedAssets(GameObject root)
    {
        var meshes = new Dictionary<Mesh, Mesh>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh source = filter.sharedMesh;
            if (source == null || AssetDatabase.Contains(source)) continue;
            Mesh saved;
            if (!meshes.TryGetValue(source, out saved))
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(
                    GeneratedFolder + "/" + Sanitize(source.name) + ".asset");
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
                    string path = AssetDatabase.GenerateUniqueAssetPath(
                        GeneratedFolder + "/" + Sanitize(source.name) + ".mat");
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
