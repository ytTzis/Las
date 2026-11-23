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
        ConfigureExperimentAudioImports();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = SphereVenueFactory.Build();
        AssignExperimentMedia(root);

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

    private static void ConfigureExperimentAudioImports()
    {
        ConfigurePcmAudio("Assets/LasVegasSphere/Media/Audio/Clap_48k_Mono.wav");
        ConfigurePcmAudio("Assets/LasVegasSphere/Media/Audio/Explosion_48k_Mono.wav");
        ConfigurePcmAudio("Assets/LasVegasSphere/Media/Audio/Aircraft_48k_Mono.wav");
    }

    private static void ConfigurePcmAudio(string path)
    {
        AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = 48000;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
    }

    private static void AssignExperimentMedia(GameObject root)
    {
        SphericalVideoScreen screen = root.GetComponentInChildren<SphericalVideoScreen>(true);
        if (screen == null) return;
        screen.ConfigureExperimentMedia(
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/LasVegasSphere/Media/Videos/Clap_60fps_Silent.mp4"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LasVegasSphere/Media/Audio/Clap_48k_Mono.wav"),
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/LasVegasSphere/Media/Videos/Explosion_60fps_Silent.mp4"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LasVegasSphere/Media/Audio/Explosion_48k_Mono.wav"),
            AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/LasVegasSphere/Media/Videos/Aircraft_60fps_Silent.mp4"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LasVegasSphere/Media/Audio/Aircraft_48k_Mono.wav"));
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
