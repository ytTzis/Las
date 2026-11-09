using System.Collections.Generic;
using UnityEngine;

public static class SphereVenueFactory
{
    public static GameObject Build(Camera existingCamera = null)
    {
        GameObject root = new GameObject("LasVegasSphereVenue");
        Material screenMaterial = MakeScreenMaterial();
        Material dark = MakeMaterial("VenueDark", new Color(0.018f, 0.022f, 0.035f), 0.15f, 0.7f);
        Material stage = MakeMaterial("Stage", new Color(0.035f, 0.045f, 0.075f), 0.65f, 0.45f);
        Material cyan = MakeMaterial("ExperimentCyan", new Color(0.03f, 0.45f, 0.75f), 0.4f, 0.2f, true);
        Material magenta = MakeMaterial("ExperimentMagenta", new Color(0.8f, 0.06f, 0.38f), 0.4f, 0.2f, true);

        CreateMeshObject("Spherical LED Screen", root.transform,
            CreateSphericalSector(28f, 34, 72, 6f, 122f, -112f, 112f), screenMaterial,
            new Vector3(0f, -6f, 6f));
        // A slightly larger shell is visible beyond the screen edges.
        CreateMeshObject("Outer Acoustic Shell", root.transform,
            CreateSphericalSector(28.35f, 24, 48, 4f, 130f, -120f, 120f), dark,
            new Vector3(0f, -6f, 6f));

        BuildFloor(root.transform, dark);
        BuildStage(root.transform, stage, cyan);
        BuildAudience(root.transform, dark);
        BuildExperiment(root.transform, cyan, magenta);
        BuildLights(root.transform);
        ConfigureCamera(existingCamera, root.transform);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.08f, 0.12f, 0.2f);
        RenderSettings.ambientEquatorColor = new Color(0.03f, 0.04f, 0.08f);
        RenderSettings.ambientGroundColor = Color.black;
        return root;
    }

    private static void BuildFloor(Transform root, Material floorMaterial)
    {
        // Unity's primitive cylinder is two units tall and one unit in diameter.
        // This places its upper face exactly at Y=0 and gives the venue a
        // continuous 29-metre-radius floor beneath the stage and seating bowl.
        GameObject floor = CreatePrimitive("Venue Floor", PrimitiveType.Cylinder, root,
            new Vector3(0f, -0.15f, 4f), new Vector3(58f, 0.15f, 58f), floorMaterial);
        floor.isStatic = true;
    }

    private static void BuildStage(Transform root, Material stageMat, Material accent)
    {
        Transform group = new GameObject("Stage").transform;
        group.SetParent(root, false);
        CreatePrimitive("Main Platform", PrimitiveType.Cylinder, group, new Vector3(0f, 0.15f, 9f), new Vector3(8f, 0.35f, 5f), stageMat);
        CreatePrimitive("Runway", PrimitiveType.Cube, group, new Vector3(0f, 0.5f, 3.5f), new Vector3(3f, 0.35f, 7f), stageMat);
        CreatePrimitive("Light Rim", PrimitiveType.Cylinder, group, new Vector3(0f, 0.55f, 9f), new Vector3(6.5f, 0.06f, 4f), accent);
    }

    private static void BuildAudience(Transform root, Material seatMat)
    {
        Transform group = new GameObject("Tiered Audience Bowl").transform;
        group.SetParent(root, false);
        Color[] people = {
            new Color(0.9f, 0.18f, 0.12f), new Color(1f, 0.55f, 0.08f),
            new Color(0.15f, 0.7f, 1f), new Color(0.7f, 0.12f, 0.9f), new Color(0.2f, 0.9f, 0.55f)
        };
        Material[] peopleMat = new Material[people.Length];
        for (int i = 0; i < people.Length; i++)
            peopleMat[i] = MakeMaterial("Audience" + i, people[i] * 0.55f, 0f, 0.5f, true);

        for (int row = 0; row < 9; row++)
        {
            float radius = 9f + row * 1.8f;
            float y = 0.35f + row * 0.68f;
            int seats = 22 + row * 5;
            for (int i = 0; i < seats; i++)
            {
                float angle = Mathf.Lerp(-128f, 128f, i / (float)(seats - 1));
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(rad) * radius, y, 9f - Mathf.Cos(rad) * radius);
                if (Mathf.Abs(p.x) < 2.3f && p.z < 6.5f) continue;

                // A stepped structural block reaches exactly to the underside of
                // every seat. Adjacent blocks overlap slightly to read as a
                // continuous raked auditorium instead of floating chairs.
                float supportHeight = Mathf.Max(0.18f, p.y - 0.16f);
                GameObject support = CreatePrimitive("Tier Support", PrimitiveType.Cube, group,
                    new Vector3(p.x, supportHeight * 0.5f, p.z),
                    new Vector3(0.78f, supportHeight, 1.9f), seatMat);
                support.transform.LookAt(new Vector3(0f, supportHeight * 0.5f, 8f));

                GameObject seat = CreatePrimitive("Seat", PrimitiveType.Cube, group, p,
                    new Vector3(0.65f, 0.32f, 0.7f), seatMat);
                seat.transform.LookAt(new Vector3(0f, y, 8f));
                if ((i + row) % 2 == 0)
                {
                    GameObject person = CreatePrimitive("Audience", PrimitiveType.Capsule, seat.transform,
                        new Vector3(0f, 0.75f, 0f), new Vector3(0.34f, 0.55f, 0.34f), peopleMat[(i + row * 2) % peopleMat.Length]);
                    person.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }

    private static void BuildExperiment(Transform root, Material leftMat, Material rightMat)
    {
        GameObject rig = new GameObject("Audio Visual Sync Experiment");
        rig.transform.SetParent(root, false);
        rig.transform.position = new Vector3(0f, 3.1f, 5.2f);
        GameObject left = CreatePrimitive("Left Collider", PrimitiveType.Sphere, rig.transform,
            new Vector3(-3.5f, 0f, 0f), new Vector3(1.25f, 1.25f, 1.25f), leftMat);
        GameObject right = CreatePrimitive("Right Collider", PrimitiveType.Sphere, rig.transform,
            new Vector3(3.5f, 0f, 0f), new Vector3(1.25f, 1.25f, 1.25f), rightMat);
        Material orbMat = MakeMaterial("ImpactOrb", new Color(0.05f, 0.25f, 0.4f), 0.2f, 0.1f, true);
        GameObject orb = CreatePrimitive("Impact Flash", PrimitiveType.Sphere, rig.transform,
            Vector3.zero, Vector3.one * 0.35f, orbMat);
        AudioSource audio = rig.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        AudioVisualSyncExperiment experiment = rig.AddComponent<AudioVisualSyncExperiment>();
        experiment.Configure(left.transform, right.transform, orb.GetComponent<Renderer>());
    }

    private static void BuildLights(Transform root)
    {
        CreateLight("Stage Key", root, new Vector3(-7f, 12f, -2f), new Color(0.25f, 0.55f, 1f), 4.2f, 32f, new Vector3(0f, 2f, 7f));
        CreateLight("Stage Fill", root, new Vector3(8f, 9f, 0f), new Color(1f, 0.12f, 0.45f), 3.5f, 35f, new Vector3(0f, 2f, 7f));
        CreateLight("Audience Glow", root, new Vector3(0f, 13f, -8f), new Color(0.35f, 0.25f, 1f), 2.2f, 75f, new Vector3(0f, 2f, 4f));
    }

    private static void ConfigureCamera(Camera camera, Transform root)
    {
        if (camera == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetParent(root, false);
            camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        camera.transform.position = new Vector3(0f, 7.4f, -19.5f);
        camera.transform.LookAt(new Vector3(0f, 5.8f, 7f));
        camera.fieldOfView = 76f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 100f;
        camera.backgroundColor = new Color(0.004f, 0.005f, 0.012f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static Mesh CreateSphericalSector(float radius, int latSegments, int lonSegments,
        float minPolar, float maxPolar, float minAzimuth, float maxAzimuth)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int y = 0; y <= latSegments; y++)
        {
            float v = y / (float)latSegments;
            float theta = Mathf.Lerp(minPolar, maxPolar, v) * Mathf.Deg2Rad;
            for (int x = 0; x <= lonSegments; x++)
            {
                float u = x / (float)lonSegments;
                float phi = Mathf.Lerp(minAzimuth, maxAzimuth, u) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Cos(phi));
                vertices.Add(direction * radius);
                normals.Add(-direction);
                uv.Add(new Vector2(u, 1f - v));
            }
        }
        int width = lonSegments + 1;
        for (int y = 0; y < latSegments; y++)
        for (int x = 0; x < lonSegments; x++)
        {
            int a = y * width + x;
            int b = a + width;
            triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
            triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
        }
        Mesh mesh = new Mesh { name = "SphereInteriorSector" };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
        return go;
    }

    private static Material MakeMaterial(string name, Color color, float metallic, float smoothness, bool emission = false)
    {
        Material mat = new Material(Shader.Find("Standard")) { name = name, color = color };
        mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Glossiness", smoothness);
        if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.5f); }
        return mat;
    }

    private static Material MakeScreenMaterial()
    {
        Shader shader = Shader.Find("LasVegasSphere/AnimatedInteriorScreen");
        if (shader == null) return MakeMaterial("FallbackScreen", new Color(0.06f, 0.25f, 0.7f), 0f, 0f, true);
        return new Material(shader) { name = "Animated Spherical LED Screen" };
    }

    private static void CreateLight(string name, Transform parent, Vector3 position, Color color, float intensity, float angle, Vector3 target)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.LookAt(target);
        Light light = go.AddComponent<Light>(); light.type = LightType.Spot; light.color = color; light.intensity = intensity;
        light.range = 45f; light.spotAngle = angle; light.shadows = LightShadows.Soft;
    }
}
