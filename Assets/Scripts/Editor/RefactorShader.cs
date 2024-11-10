using UnityEditor;
using UnityEngine;
using System.IO;

public class TurnOnShadows : EditorWindow
{
    private Shader shaderAsset;
    private Shader otterRampShaderAsset;

    private Material matAsset;

    // Add menu named "Shader Material Actions" to the Window menu
    [MenuItem("重构/Shader")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        TurnOnShadows window = (TurnOnShadows)GetWindow(typeof(TurnOnShadows));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        shaderAsset = (Shader)EditorGUILayout.ObjectField("Drag Shader Here", shaderAsset, typeof(Shader), false);
        otterRampShaderAsset = (Shader)EditorGUILayout.ObjectField("Drag Otter Ramp Shader Here", otterRampShaderAsset, typeof(Shader), false);
       
        EditorGUI.BeginDisabledGroup(shaderAsset == null || otterRampShaderAsset == null);
        if (GUILayout.Button("Turn On Built-in Shadows for All Materials using this shader"))
        {
            PerformActionOnMaterials();
        }
        EditorGUI.EndDisabledGroup();

        
        matAsset = (Material)EditorGUILayout.ObjectField("Drag Single Material Here", matAsset, typeof(Material), false);

        EditorGUI.BeginDisabledGroup(matAsset == null);
        if (GUILayout.Button("Debug"))
        {
            PerformActionSingleObj(matAsset);
        }
        EditorGUI.EndDisabledGroup();
    }

    void PerformActionOnMaterials()
    {
        // Early out if no shader has been dragged
        if (shaderAsset == null)
        {
            Debug.LogError("No shader assigned!");
            return;
        }

        string shaderName = shaderAsset.name;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                PerformActionSingleObj(material);
            }
        }

        // Save all modified assets
        AssetDatabase.SaveAssets();
        Debug.Log("Action performed on all materials with shader: " + shaderName);
    }

    void PerformActionSingleObj(Material mat)
    {
        Texture2D GenerateStepTexture()
        {
            const int numSteps = 3;
            var t2d = new Texture2D(numSteps + 1, /*height=*/1, TextureFormat.R8, /*mipChain=*/false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int i = 0; i < numSteps + 1; i++)
            {
                var color = Color.white * i / numSteps;
                t2d.SetPixel(i, 0, color);
            }

            t2d.Apply();
            return t2d;
        }

        void SaveTex(Texture2D tex, string pathRelativeToAssets, string fullPath)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.Refresh();
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(pathRelativeToAssets);

            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.textureType = TextureImporterType.SingleChannel;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                var textureSettings = new TextureImporterPlatformSettings
                {
                    format = TextureImporterFormat.R8
                };
                importer.SetPlatformTextureSettings(textureSettings);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            Debug.Log($"created {fullPath}");
        }

        if (mat == null)
        {
            Debug.LogError("No mat found!");
            return;
        }

        if (mat.shader == shaderAsset)
        {
            Undo.RecordObject(mat, "Enable Shadow");
            mat.SetInt("_UnityShadowMode", 2);
            mat.SetColor("_UnityShadowColor", Color.black);
            mat.DisableKeyword("_UNITYSHADOWMODE_NONE");
            mat.EnableKeyword("_UNITYSHADOWMODE_COLOR");
            EditorUtility.SetDirty(mat);
        }
        else if (mat.shader == otterRampShaderAsset)
        {
            Undo.RecordObject(mat, "Change Shader");
            var brightColor = mat.GetColor("_BrightenColor");
            var darkColor = mat.GetColor("_DarkColor");

            // change to stylized shader
            mat.shader = shaderAsset;
            var tex = GenerateStepTexture();
            
            var dir = AssetDatabase.GetAssetPath(mat);
            dir = Path.GetDirectoryName(dir);
            var pngNameNoExtension = string.Format("{0}_CelStepTexture-ramp", mat.name);
            var relPath = Path.Combine(
                dir,
                pngNameNoExtension + ".png"
            );
            var fullPath = Path.Combine(Application.dataPath, 
                dir.Substring(dir.IndexOf(Path.DirectorySeparatorChar) + 1),
                pngNameNoExtension + ".png"
            );

            SaveTex(tex, relPath, fullPath);

            mat.SetColor("_BaseColor", brightColor);

            mat.SetInt("_CelPrimaryMode", 2);

            mat.DisableKeyword("_CELPRIMARYMODE_SINGLE");
            mat.DisableKeyword("_CELPRIMARYMODE_CURVE");
            mat.EnableKeyword("_CELPRIMARYMODE_STEPS");
            mat.SetColor("_ColorDimSteps", darkColor);

            mat.SetTexture("_CelStepTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(relPath));
            mat.SetInt("_UnityShadowMode", 2);
            mat.DisableKeyword("_UNITYSHADOWMODE_NONE");
            mat.EnableKeyword("_UNITYSHADOWMODE_COLOR");
            EditorUtility.SetDirty(mat);
        }
    }
}
