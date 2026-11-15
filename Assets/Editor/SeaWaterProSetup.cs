#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Put in Assets/Editor/. Run: Tools > BeachTidy > Setup Sea Water (Pro)
public static class SeaWaterProSetup
{
    const string TexFolder  = "Assets/Textures/Sea/";
    const string MatPath    = "Assets/Shaders/SeaWaterPro.mat";
    const string ShaderName = "Universal Render Pipeline/Custom/BeachTidySea";
    const string WaterName  = "Water";   // name of your sea plane in the Hierarchy

    [MenuItem("Tools/BeachTidy/Setup Sea Water (Pro)")]
    static void Setup()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError("Shader not found: " + ShaderName + ". Put BeachTidySea.shader in the project and check the Console for shader errors.");
            return;
        }

        Texture2D wave = ImportTexture("Sea_WaveHeightMap.png", false, false); // data: sRGB off, uncompressed
        Texture2D foam = ImportTexture("Sea_Foam.png", false, true);           // used as a mask: sRGB off

        // Material (created once; later runs only refresh the textures so your tweaks are kept)
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.shader = shader;
        mat.SetTexture("_WaveMap", wave);
        mat.SetTexture("_FoamTex", foam);
        EditorUtility.SetDirty(mat);

        FixWaterObject(mat);
        EnableDepthTexture();

        AssetDatabase.SaveAssets();
        Debug.Log("Sea set up. Material: " + MatPath);
    }

    static void FixWaterObject(Material mat)
    {
        GameObject water = GameObject.Find(WaterName);
        if (water == null)
        {
            Debug.LogWarning("No active object named '" + WaterName + "' found. Assign " + MatPath + " to your sea plane yourself, set Cast Shadows to Off and remove its collider.");
            return;
        }

        MeshRenderer mr = water.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Undo.RecordObject(mr, "Sea setup");
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        Collider col = water.GetComponent<Collider>();
        if (col != null) Undo.DestroyObjectImmediate(col);

        EditorUtility.SetDirty(water);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(water.scene);
    }

    // The shader reads the scene depth, so URP must produce a depth texture.
    static void EnableDepthTexture()
    {
        var assets = new HashSet<RenderPipelineAsset>();
        if (GraphicsSettings.defaultRenderPipeline != null)
            assets.Add(GraphicsSettings.defaultRenderPipeline);

        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            RenderPipelineAsset a = QualitySettings.GetRenderPipelineAssetAt(i);
            if (a != null) assets.Add(a);
        }

        if (assets.Count == 0)
        {
            Debug.LogWarning("No URP asset found. Is this a URP project?");
            return;
        }

        foreach (RenderPipelineAsset a in assets)
        {
            var so = new SerializedObject(a);
            SerializedProperty p = so.FindProperty("m_RequireDepthTexture");
            if (p != null)
            {
                p.boolValue = true;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(a);
            }
            else
            {
                Debug.LogWarning("Could not set Depth Texture on '" + a.name + "'. Select that asset and tick 'Depth Texture' by hand.");
            }
        }
    }

    static Texture2D ImportTexture(string file, bool srgb, bool compress)
    {
        string path = TexFolder + file;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("Texture not found: " + path);
            return null;
        }

        imp.textureType = TextureImporterType.Default;
        imp.sRGBTexture = srgb;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = true;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = false;
        imp.textureCompression = compress
            ? TextureImporterCompression.Compressed
            : TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
