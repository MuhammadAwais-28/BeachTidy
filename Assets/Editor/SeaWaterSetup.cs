#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Put this file in a folder named "Editor" (e.g. Assets/Editor/SeaWaterSetup.cs).
// Put the 4 PNGs in Assets/Textures/Sea/.
// Then run: Tools > BeachTidy > Setup Sea Water Material
public static class SeaWaterSetup
{
    const string TexFolder  = "Assets/Textures/Sea/";
    const string MatPath    = "Assets/Shaders/SeaWater.mat";
    const string ShaderName = "Universal Render Pipeline/Custom/MobileWaterShaderEKV_URP";

    // Plane width in world units (Unity Plane = 10 units x scale). Used for tiling + foam width.
    const float PlaneWidth  = 1000f;

    [MenuItem("Tools/BeachTidy/Setup Sea Water Material")]
    static void Setup()
    {
        Texture2D a    = Import("Sea_TextureA.png",        srgb: true,  compress: true);
        Texture2D flow = Import("Sea_FlowMap_TextureB.png", srgb: false, compress: false);
        Texture2D wave = Import("Sea_WaveHeightMap.png",    srgb: false, compress: false);
        Texture2D foam = Import("Sea_Foam.png",             srgb: true,  compress: true);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            Debug.LogError("SeaWater material not found at " + MatPath);
            return;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError("Shader not found: " + ShaderName);
            return;
        }
        mat.shader = shader;

        // Textures
        mat.SetTexture("_MainTex", a);
        mat.SetTexture("_DiffTex", flow);
        mat.SetTexture("_DerivHeightMap", wave);
        mat.SetTexture("_FoamTex", foam);

        // Tiling: roughly one texture repeat per 25 world units
        float tiling = Mathf.Max(1f, PlaneWidth / 25f);
        mat.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
        mat.SetTextureScale("_DiffTex", new Vector2(tiling, tiling));
        mat.SetTextureScale("_FoamTex", Vector2.one);   // keep 1,1: this drives the edge-foam mask

        // Look
        mat.SetColor("_Color", new Color(0.15f, 0.55f, 0.85f, 0.55f));
        mat.SetFloat("_Shininess", 0.3f);
        mat.SetFloat("_Brightness", 1f);
        mat.SetFloat("_Tiling", 1f);          // wave tiling (multiplies the texture tiling)
        mat.SetFloat("_Speed", 0.1f);
        mat.SetFloat("_FlowStrength", 0.1f);

        // Foam on the plane edges. Width is in UV units: world metres / plane width.
        mat.SetFloat("_FoamWidth", 3f / PlaneWidth);   // about 3 m wide band
        mat.SetFloat("_FoamIntensity", 3f);

        // Toggles: set the float AND the keyword (SetFloat alone doesn't change keywords)
        SetToggle(mat, "_SECTIONLIGHTING", "LIGHTING", true);
        SetToggle(mat, "_SECTIONTEXTURES", "TEXTURES", true);
        SetToggle(mat, "_SECTIONWAVES",    "WAVES",    true);
        SetToggle(mat, "_FOAM",            "FOAM",     true);
        SetToggle(mat, "_Invert",          "INVERT",   false);  // foam on edges
        SetToggle(mat, "_Squared",         "SQUARED",  true);   // square edge mask
        SetToggle(mat, "_Reflection",      "REFLECTION", false); // no cubemap yet

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("SeaWater material set up. Now assign it to your Sea plane (Cast Shadows = Off).");
    }

    static void SetToggle(Material m, string floatName, string keyword, bool on)
    {
        m.SetFloat(floatName, on ? 1f : 0f);
        if (on) m.EnableKeyword(keyword); else m.DisableKeyword(keyword);
    }

    static Texture2D Import(string file, bool srgb, bool compress)
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
        imp.alphaIsTransparency = false;   // alpha holds data, don't dilate edges
        imp.textureCompression = compress
            ? TextureImporterCompression.Compressed
            : TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
