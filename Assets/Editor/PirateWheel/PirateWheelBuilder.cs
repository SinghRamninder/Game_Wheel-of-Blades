using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the wooden pirate wheel mesh, its textures and materials, and assigns them to the Wheel.
// The geometry is built in a "wheel frame" (axle = world Z, centered on the wheel pivot, meters)
// and then converted into the Wheel's local space, so its existing rotation/scale and the
// WheelController radius logic stay untouched.
public static class PirateWheelBuilder
{
    private const string Folder = "Assets/Art/Wheel";
    private const string SettingsPath = Folder + "/PirateWheelSettings.asset";
    private const string MeshPath = Folder + "/PirateWheel.asset";
    private const string WoodMatPath = Folder + "/Wood.mat";
    private const string IronMatPath = Folder + "/Iron.mat";
    private const string WoodAlbedoPath = Folder + "/Wood_Albedo.png";
    private const string WoodNormalPath = Folder + "/Wood_Normal.png";
    private const string IronAlbedoPath = Folder + "/Iron_Albedo.png";
    private const string IronMaskPath = Folder + "/Iron_MetallicSmoothness.png";

    private const int Wood = 0;
    private const int Iron = 1;

    [MenuItem("Tools/Pirate Wheel/Build Wheel Model")]
    public static void BuildFromMenu() => Build(GetOrCreateSettings(), false);

    [MenuItem("Tools/Pirate Wheel/Regenerate Textures + Build")]
    public static void RebuildAllFromMenu() => Build(GetOrCreateSettings(), true);

    [MenuItem("Tools/Pirate Wheel/Select Settings")]
    public static void SelectSettings() => Selection.activeObject = GetOrCreateSettings();

    public static PirateWheelSettings GetOrCreateSettings()
    {
        EnsureFolder();
        var settings = AssetDatabase.LoadAssetAtPath<PirateWheelSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PirateWheelSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    public static string Build(PirateWheelSettings s, bool regenerateTextures)
    {
        EnsureFolder();

        Transform target = FindTarget(s);
        if (target == null)
        {
            Debug.LogError($"[PirateWheel] No WheelController / GameObject named '{s.targetObjectName}' in the open scene.");
            return "target not found";
        }

        if (regenerateTextures || !File.Exists(WoodAlbedoPath) || !File.Exists(IronAlbedoPath))
        {
            GenerateWoodTextures(s);
            GenerateIronTextures(s);
        }

        Material wood = CreateWoodMaterial();
        Material iron = CreateIronMaterial();
        Mesh mesh = BuildMesh(s, target);

        var mf = target.GetComponent<MeshFilter>();
        var mr = target.GetComponent<MeshRenderer>();
        if (mf == null) mf = Undo.AddComponent<MeshFilter>(target.gameObject);
        if (mr == null) mr = Undo.AddComponent<MeshRenderer>(target.gameObject);
        Undo.RecordObjects(new Object[] { mf, mr }, "Build Pirate Wheel");
        mf.sharedMesh = mesh;
        mr.sharedMaterials = new[] { wood, iron };
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);

        string info = $"[PirateWheel] Built '{target.name}': walk radius {target.lossyScale.x * 0.5f:F3} m, " +
                      $"{mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris.";
        Debug.Log(info);
        return info;
    }

    private static Transform FindTarget(PirateWheelSettings s)
    {
        var controller = Object.FindFirstObjectByType<WheelController>();
        if (controller != null) return controller.transform;
        var go = GameObject.Find(s.targetObjectName);
        return go != null ? go.transform : null;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "Wheel");
    }

    #region Mesh

    private class MeshData
    {
        public readonly List<Vector3> Verts = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int>[] Tris = { new List<int>(), new List<int>() };

        public int Add(Vector3 p, Vector3 n, Vector2 uv)
        {
            Verts.Add(p);
            Normals.Add(n);
            Uvs.Add(uv);
            return Verts.Count - 1;
        }

        // Adds a triangle and flips its winding if needed so it faces 'facing'.
        public void Tri(int sub, int a, int b, int c, Vector3 facing)
        {
            Vector3 cross = Vector3.Cross(Verts[b] - Verts[a], Verts[c] - Verts[a]);
            if (Vector3.Dot(cross, facing) < 0f) (b, c) = (c, b);
            Tris[sub].Add(a);
            Tris[sub].Add(b);
            Tris[sub].Add(c);
        }

        // a,b = one edge; c,d = the next edge (a->c and b->d are parallel).
        public void Quad(int sub, int a, int b, int c, int d)
        {
            Vector3 facing = Normals[a] + Normals[b] + Normals[c] + Normals[d];
            Tri(sub, a, c, b, facing);
            Tri(sub, b, c, d, facing);
        }
    }

    private struct Frame
    {
        public Vector3 Origin, X, Y, Z;
        public static readonly Frame Wheel = new Frame { Origin = Vector3.zero, X = Vector3.right, Y = Vector3.up, Z = Vector3.forward };

        public static Frame Along(Vector3 origin, Vector3 axis)
        {
            Vector3 z = axis.normalized;
            Vector3 x = Vector3.Cross(z, Mathf.Abs(z.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 y = Vector3.Cross(z, x);
            return new Frame { Origin = origin, X = x, Y = y, Z = z };
        }

        public Vector3 Point(float r, float angle, float z) =>
            Origin + X * (r * Mathf.Cos(angle)) + Y * (r * Mathf.Sin(angle)) + Z * z;

        public Vector3 Dir(float r, float angle, float z) =>
            X * (r * Mathf.Cos(angle)) + Y * (r * Mathf.Sin(angle)) + Z * z;
    }

    private static Mesh BuildMesh(PirateWheelSettings s, Transform target)
    {
        var m = new MeshData();
        var rng = new System.Random(s.seed);
        float RandomOffset() => (float)rng.NextDouble();

        float R = target.lossyScale.x * 0.5f;        // walking surface, same as WheelController.radius
        float H = s.flangeHeight;
        float F = s.flangeThickness;
        float T = s.grooveWidth + 2f * F;              // total rim thickness
        float Ri = R - s.rimDepth;                     // inner rim radius (spokes end here)
        float b = Mathf.Min(s.bevel, F * 0.4f, H * 0.4f);
        float z0 = -T * 0.5f;                          // front face (towards camera)
        float z1 = T * 0.5f;                           // back face
        float tw = s.woodTextureWorldSize;
        float ti = s.ironTextureWorldSize;

        // Rim cross-section (r, z), closed loop. Going front face -> front flange -> groove floor ->
        // back flange -> back face -> inner surface keeps normals = (dz, -dr) pointing outward.
        var rim = new[]
        {
            new Vector2(Ri + b, z0),
            new Vector2(R + H - b, z0),
            new Vector2(R + H, z0 + b),
            new Vector2(R + H, z0 + F - b),
            new Vector2(R + H - b, z0 + F),
            new Vector2(R, z0 + F),
            new Vector2(R, z1 - F),
            new Vector2(R + H - b, z1 - F),
            new Vector2(R + H, z1 - F + b),
            new Vector2(R + H, z1 - b),
            new Vector2(R + H - b, z1),
            new Vector2(Ri + b, z1),
            new Vector2(Ri, z1 - b),
            new Vector2(Ri, z0 + b),
        };

        // Felloes: joints sit halfway between spokes, like a real cart wheel.
        float jointOffset = s.spokeCount > 0 ? Mathf.PI / s.spokeCount : 0f;
        float felloeArc = 2f * Mathf.PI / s.felloeCount;
        for (int f = 0; f < s.felloeCount; f++)
        {
            float a0 = jointOffset + f * felloeArc;
            Revolve(m, Wood, Frame.Wheel, rim, true, a0, a0 + felloeArc, s.segmentsPerFelloe,
                true, tw, new Vector2(RandomOffset(), RandomOffset()));
        }

        int fullSegs = s.segmentsPerFelloe * s.felloeCount;

        // Iron hoops on top of both flanges.
        if (s.ironTire)
        {
            float rIn = R + H - 0.003f, rOut = R + H + s.tireThickness;
            float inset = b * 0.5f;
            foreach (float zs in new[] { z0, z1 - F })
            {
                var hoop = new[]
                {
                    new Vector2(rIn, zs + inset - 0.002f),
                    new Vector2(rOut, zs + inset - 0.002f),
                    new Vector2(rOut, zs + F - inset + 0.002f),
                    new Vector2(rIn, zs + F - inset + 0.002f),
                };
                Revolve(m, Iron, Frame.Wheel, hoop, false, 0f, 2f * Mathf.PI, fullSegs, true, ti, Vector2.zero);
            }
        }

        // Iron clamp plates (with bolts) over every felloe joint, front and back.
        if (s.jointPlates)
        {
            float rOuter = R + H;
            float rm = (Ri + rOuter) * 0.5f;
            float halfR = (rOuter - Ri) * 0.5f - 0.012f;
            const float plateT = 0.008f;
            for (int f = 0; f < s.felloeCount; f++)
            {
                float a = jointOffset + f * felloeArc;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var tan = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
                foreach (float side in new[] { -1f, 1f })
                {
                    float faceZ = side * T * 0.5f;
                    Box(m, Iron, dir * rm + Vector3.forward * (faceZ + side * (plateT * 0.5f - 0.001f)),
                        dir, tan, Vector3.forward, new Vector3(halfR, 0.034f, plateT * 0.5f), ti);
                    foreach (float rb in new[] { Ri + 0.035f, rOuter - 0.035f })
                    {
                        Vector3 basePos = dir * rb + Vector3.forward * (faceZ + side * (plateT - 0.002f));
                        Bolt(m, Frame.Along(basePos, Vector3.forward * side), 0.012f, 0.009f, 6, ti);
                    }
                }
            }
        }

        // Spokes.
        for (int k = 0; k < s.spokeCount; k++)
        {
            float a = k * 2f * Mathf.PI / s.spokeCount;
            Spoke(m, a, s.hubRadius - 0.02f, Ri + 0.02f, s.spokeWidthAtHub, s.spokeWidthAtRim,
                s.spokeDepthAtHub, s.spokeDepthAtRim, 12, tw, new Vector2(RandomOffset(), RandomOffset()));
        }

        // Hub barrel (grain runs along the axle).
        float hr = s.hubRadius, hl = s.hubLength * 0.5f, hc = Mathf.Min(0.03f, hr * 0.3f);
        var hub = new[]
        {
            new Vector2(0f, -hl),
            new Vector2(hr - hc, -hl),
            new Vector2(hr, -hl + hc),
            new Vector2(hr, hl - hc),
            new Vector2(hr - hc, hl),
            new Vector2(0f, hl),
        };
        Revolve(m, Wood, Frame.Wheel, hub, false, 0f, 2f * Mathf.PI, 48, false, tw, new Vector2(RandomOffset(), RandomOffset()));

        // Iron bands around the hub.
        foreach (float zc in new[] { -hl + 0.06f, hl - 0.06f })
        {
            var band = new[]
            {
                new Vector2(hr - 0.002f, zc - 0.016f),
                new Vector2(hr + 0.008f, zc - 0.016f),
                new Vector2(hr + 0.008f, zc + 0.016f),
                new Vector2(hr - 0.002f, zc + 0.016f),
            };
            Revolve(m, Iron, Frame.Wheel, band, false, 0f, 2f * Mathf.PI, 48, true, ti, Vector2.zero);
        }

        // Iron hub cap, axle nut and cap bolts (front only, the camera never sees the back).
        float cr = s.capRadius, capFront = -hl - 0.025f;
        var cap = new[]
        {
            new Vector2(0f, capFront),
            new Vector2(cr - 0.012f, capFront),
            new Vector2(cr, capFront + 0.013f),
            new Vector2(cr, -hl + 0.005f),
        };
        Revolve(m, Iron, Frame.Wheel, cap, false, 0f, 2f * Mathf.PI, 40, true, ti, Vector2.zero);
        Bolt(m, Frame.Along(new Vector3(0f, 0f, capFront + 0.002f), Vector3.back), 0.032f, 0.022f, 6, ti);
        for (int k = 0; k < 6; k++)
        {
            float a = (k + 0.5f) * Mathf.PI / 3f;
            var p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (cr * 0.7f) + Vector3.forward * (capFront + 0.002f);
            Bolt(m, Frame.Along(p, Vector3.back), 0.009f, 0.007f, 6, ti);
        }

        return WriteMesh(m, target);
    }

    // Revolves a 2D (r, z) profile around frame.Z between angles a0..a1.
    // grainAround: wood grain follows the circumference (felloes); otherwise it follows the profile (hub).
    private static void Revolve(MeshData m, int sub, Frame fr, Vector2[] prof, bool closed, float a0, float a1,
        int segs, bool grainAround, float texWorld, Vector2 uvOffset)
    {
        int bands = closed ? prof.Length : prof.Length - 1;
        float dist = 0f;
        for (int i = 0; i < bands; i++)
        {
            Vector2 p0 = prof[i];
            Vector2 p1 = prof[(i + 1) % prof.Length];
            Vector2 d = p1 - p0;
            float len = d.magnitude;
            if (len < 1e-6f) continue;
            var n2 = new Vector2(d.y, -d.x) / len;

            int start = m.Verts.Count;
            for (int j = 0; j <= segs; j++)
            {
                float a = Mathf.Lerp(a0, a1, (float)j / segs);
                for (int k = 0; k < 2; k++)
                {
                    Vector2 p = k == 0 ? p0 : p1;
                    float pd = dist + (k == 0 ? 0f : len);
                    float around = (a - a0) * p.x;
                    Vector2 uv = grainAround
                        ? new Vector2(around / texWorld, pd / texWorld)
                        : new Vector2(pd / texWorld, around / texWorld);
                    m.Add(fr.Point(p.x, a, p.y), fr.Dir(n2.x, a, n2.y).normalized, uv + uvOffset);
                }
            }
            for (int j = 0; j < segs; j++)
            {
                int a = start + j * 2;
                m.Quad(sub, a, a + 1, a + 2, a + 3);
            }
            dist += len;
        }
    }

    // Tapered elliptical spoke from radius r0 to r1 at angle 'angle'. Width is seen from the camera,
    // depth runs along the axle. UV seam is placed on the back side.
    private static void Spoke(MeshData m, float angle, float r0, float r1, float w0, float w1, float d0, float d1,
        int sides, float texWorld, Vector2 uvOffset)
    {
        var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        var tan = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);
        Vector3 fwd = Vector3.forward;
        const int rings = 6;
        int start = m.Verts.Count;
        for (int i = 0; i <= rings; i++)
        {
            float t = (float)i / rings;
            float bulge = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
            float ha = Mathf.Lerp(w0, w1, t) * 0.5f * bulge;
            float hb = Mathf.Lerp(d0, d1, t) * 0.5f * bulge;
            float r = Mathf.Lerp(r0, r1, t);
            float perimeter = Mathf.PI * (ha + hb);
            for (int k = 0; k <= sides; k++)
            {
                float th = Mathf.PI * 0.5f + k * 2f * Mathf.PI / sides;
                float c = Mathf.Cos(th), sn = Mathf.Sin(th);
                Vector3 p = dir * r + tan * (c * ha) + fwd * (sn * hb);
                Vector3 n = (tan * (c / ha) + fwd * (sn / hb)).normalized;
                var uv = new Vector2(r / texWorld, (float)k / sides * perimeter / texWorld);
                m.Add(p, n, uv + uvOffset);
            }
        }
        int row = sides + 1;
        for (int i = 0; i < rings; i++)
        for (int k = 0; k < sides; k++)
        {
            int a = start + i * row + k;
            m.Quad(Wood, a, a + 1, a + row, a + row + 1);
        }
    }

    private static void Box(MeshData m, int sub, Vector3 c, Vector3 ax, Vector3 ay, Vector3 az, Vector3 half, float texWorld)
    {
        Vector3[] axes = { ax, ay, az };
        float[] h = { half.x, half.y, half.z };
        for (int i = 0; i < 3; i++)
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            Vector3 n = axes[i] * sgn;
            Vector3 u = axes[(i + 1) % 3], v = axes[(i + 2) % 3];
            float hu = h[(i + 1) % 3], hv = h[(i + 2) % 3];
            Vector3 fc = c + n * h[i];
            int a = m.Add(fc - u * hu - v * hv, n, new Vector2(-hu, -hv) / texWorld);
            m.Add(fc - u * hu + v * hv, n, new Vector2(-hu, hv) / texWorld);
            m.Add(fc + u * hu - v * hv, n, new Vector2(hu, -hv) / texWorld);
            m.Add(fc + u * hu + v * hv, n, new Vector2(hu, hv) / texWorld);
            m.Quad(sub, a, a + 1, a + 2, a + 3);
        }
    }

    // Domed bolt head standing on frame origin, pointing along frame.Z.
    private static void Bolt(MeshData m, Frame fr, float radius, float height, int sides, float texWorld)
    {
        var prof = new[]
        {
            new Vector2(radius, -0.002f),
            new Vector2(radius, height * 0.6f),
            new Vector2(radius * 0.65f, height),
            new Vector2(0f, height * 1.05f),
        };
        Revolve(m, Iron, fr, prof, false, 0f, 2f * Mathf.PI, sides, true, texWorld, Vector2.zero);
    }

    private static Mesh WriteMesh(MeshData m, Transform target)
    {
        // Wheel frame -> target local space (ignoring position, the frame is centered on the pivot).
        Matrix4x4 toWorld = target.localToWorldMatrix;
        toWorld.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
        Matrix4x4 toLocal = toWorld.inverse;
        Matrix4x4 normalToLocal = toWorld.transpose;

        var verts = new Vector3[m.Verts.Count];
        var normals = new Vector3[m.Verts.Count];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = toLocal.MultiplyVector(m.Verts[i]);
            normals[i] = normalToLocal.MultiplyVector(m.Normals[i]).normalized;
        }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh();
        mesh.Clear();
        mesh.name = "PirateWheel";
        mesh.indexFormat = verts.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = m.Uvs.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(m.Tris[Wood], Wood);
        mesh.SetTriangles(m.Tris[Iron], Iron);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        if (isNew) AssetDatabase.CreateAsset(mesh, MeshPath);
        else EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    #endregion

    #region Materials

    private static Material LoadOrCreateMaterial(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CreateWoodMaterial()
    {
        Material mat = LoadOrCreateMaterial(WoodMatPath);
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodAlbedoPath);
        mat.SetTexture("_BaseMap", albedo);
        mat.mainTexture = albedo;
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(WoodNormalPath));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.22f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateIronMaterial()
    {
        Material mat = LoadOrCreateMaterial(IronMatPath);
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(IronAlbedoPath);
        mat.SetTexture("_BaseMap", albedo);
        mat.mainTexture = albedo;
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(IronMaskPath));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Metallic", 1f);
        mat.SetFloat("_Smoothness", 1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    #endregion

    #region Textures

    // Tileable wood: grain runs along U. Periodic value noise keeps every layer seamless.
    private static void GenerateWoodTextures(PirateWheelSettings s)
    {
        int size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(s.woodTextureSize, 256, 4096));
        int seed = s.seed;
        int rings = Mathf.Max(1, s.woodRings);
        var albedo = new Color32[size * size];
        var height = new float[size * size];
        Color light = s.woodLight, dark = s.woodDark;

        Parallel.For(0, size, y =>
        {
            float t = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float warp = Fbm(u * 3f, t * 6f, 3, 6, 4, seed) - 0.5f;
                float warp2 = Fbm(u * 1f, t * 2f, 1, 2, 3, seed + 100) - 0.5f;
                float ringCoord = t * rings + warp * 3f + warp2 * 6f;
                float ring = ringCoord - Mathf.Floor(ringCoord);
                float late = ring * ring * ring * ring;
                float fibers = Fbm(u * 8f, t * 256f, 8, 256, 3, seed + 200);
                float tone = Fbm(u * 2f, t * 2f, 2, 2, 4, seed + 300);
                float grit = ValueNoise(u * 512f, t * 512f, 512, 512, seed + 400);

                float d = Mathf.Clamp01(0.1f + 0.5f * late + 0.35f * fibers + 0.45f * (tone - 0.5f) + 0.1f * (grit - 0.5f));
                Color c = Color.Lerp(light, dark, d) * Mathf.Lerp(0.78f, 1.08f, tone);
                c.a = 1f;
                int i = y * size + x;
                albedo[i] = c;
                height[i] = 1f - 0.6f * late - 0.4f * fibers + 0.1f * grit;
            }
        });

        WriteTexture(WoodAlbedoPath, size, albedo, TextureKind.Color);
        WriteTexture(WoodNormalPath, size, HeightToNormal(height, size, 3f), TextureKind.Normal);
    }

    private static void GenerateIronTextures(PirateWheelSettings s)
    {
        const int size = 512;
        int seed = s.seed + 1000;
        var albedo = new Color32[size * size];
        var mask = new Color32[size * size];
        Color ironCol = s.ironColor, rustCol = s.rustColor;

        Parallel.For(0, size, y =>
        {
            float t = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float n = Fbm(u * 4f, t * 4f, 4, 4, 5, seed);
                float detail = Fbm(u * 32f, t * 32f, 32, 32, 2, seed + 50);
                float rust = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, (n - 0.52f) / 0.2f) * (0.6f + 0.4f * detail));
                float pit = ValueNoise(u * 256f, t * 256f, 256, 256, seed + 90);

                Color c = Color.Lerp(ironCol, rustCol, rust) * (0.85f + 0.3f * pit);
                c.a = 1f;
                int i = y * size + x;
                albedo[i] = c;
                float metal = Mathf.Lerp(0.9f, 0.2f, rust);
                float smooth = Mathf.Lerp(0.5f, 0.12f, rust) * (0.8f + 0.2f * pit);
                mask[i] = new Color(metal, 0f, 0f, smooth);
            }
        });

        WriteTexture(IronAlbedoPath, size, albedo, TextureKind.Color);
        WriteTexture(IronMaskPath, size, mask, TextureKind.Linear);
    }

    private static Color32[] HeightToNormal(float[] h, int size, float strength)
    {
        var result = new Color32[size * size];
        Parallel.For(0, size, y =>
        {
            int yu = (y + 1) % size, yd = (y - 1 + size) % size;
            for (int x = 0; x < size; x++)
            {
                int xr = (x + 1) % size, xl = (x - 1 + size) % size;
                float dx = (h[y * size + xr] - h[y * size + xl]) * strength;
                float dy = (h[yu * size + x] - h[yd * size + x]) * strength;
                Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                result[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        });
        return result;
    }

    private enum TextureKind { Color, Linear, Normal }

    private static void WriteTexture(string path, int size, Color32[] pixels, TextureKind kind)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, kind != TextureKind.Color);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = kind == TextureKind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = kind == TextureKind.Color;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }

    private static int Wrap(int v, int p) => ((v % p) + p) % p;

    // Value noise that repeats every (px, py) lattice cells.
    private static float ValueNoise(float x, float y, int px, int py, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        int x0 = Wrap(xi, px), x1 = Wrap(xi + 1, px);
        int y0 = Wrap(yi, py), y1 = Wrap(yi + 1, py);
        float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
        float a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), sx);
        float b = Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), sx);
        return Mathf.Lerp(a, b, sy);
    }

    private static float Fbm(float x, float y, int px, int py, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * ValueNoise(x, y, px, py, seed + o * 31);
            norm += amp;
            x *= 2f; y *= 2f; px *= 2; py *= 2;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    #endregion
}

[CustomEditor(typeof(PirateWheelSettings))]
public class PirateWheelSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Walking radius comes from the Wheel's scale (lossyScale.x / 2), same as WheelController.", MessageType.Info);
        if (GUILayout.Button("Build / Update Wheel Model", GUILayout.Height(30)))
            PirateWheelBuilder.Build((PirateWheelSettings)target, false);
        if (GUILayout.Button("Regenerate Textures + Build"))
            PirateWheelBuilder.Build((PirateWheelSettings)target, true);
    }
}
