#if GRIFFIN
using Pinwheel.Griffin.BackupTool;
using Pinwheel.Griffin.BillboardTool;
using Pinwheel.Griffin.DataTool;
using Pinwheel.Griffin.ExtensionSystem;
using Pinwheel.Griffin.GroupTool;
using Pinwheel.Griffin.HelpTool;
using Pinwheel.Griffin.PaintTool;
using Pinwheel.Griffin.SplineTool;
using Pinwheel.Griffin.StampTool;
using Pinwheel.Griffin.TextureTool;
using Pinwheel.Griffin.ErosionTool;
using Pinwheel.Griffin.Physic;
using Pinwheel.Griffin.Wizard;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Griffin
{
    public static class GEditorMenus
    {
        [MenuItem("GameObject/3D Object/Polaris/Terrain Wizard", false, -1000)]
        public static void ShowTerrainWizard(MenuCommand menuCmd)
        {
            GWizardWindow.ShowMainPage(menuCmd);
        }

        [MenuItem("GameObject/3D Object/Polaris/Basic Tools", false, -999)]
        public static void CreateTerrainTools(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Polaris Tools");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            g.transform.hideFlags = HideFlags.HideInInspector;

            GTerrainTools tools = g.AddComponent<GTerrainTools>();
            tools.hideFlags = HideFlags.HideInInspector;

            GameObject terrainGroup = new GameObject("Terrain Group");
            terrainGroup.transform.parent = g.transform;
            GUtilities.ResetTransform(terrainGroup.transform, g.transform);
            terrainGroup.transform.hideFlags = HideFlags.HideInInspector;
            GTerrainGroup group = terrainGroup.AddComponent<GTerrainGroup>();
            group.GroupId = -1;

            GameObject texturePainter = new GameObject("Geometry & Texture Painter");
            texturePainter.transform.parent = g.transform;
            GUtilities.ResetTransform(texturePainter.transform, g.transform);
            texturePainter.transform.hideFlags = HideFlags.HideInInspector;
            GTerrainTexturePainter texturePainterComponent = texturePainter.AddComponent<GTerrainTexturePainter>();
            texturePainterComponent.GroupId = -1;

            GameObject foliagePainter = new GameObject("Foliage Painter");
            foliagePainter.transform.parent = g.transform;
            GUtilities.ResetTransform(foliagePainter.transform, g.transform);
            foliagePainter.transform.hideFlags = HideFlags.HideInInspector;
            GFoliagePainter foliagePainterComponent = foliagePainter.AddComponent<GFoliagePainter>();
            foliagePainterComponent.GroupId = -1;
            foliagePainterComponent.gameObject.AddComponent<GRotationRandomizeFilter>();
            foliagePainterComponent.gameObject.AddComponent<GScaleRandomizeFilter>();

            GameObject objectPainter = new GameObject("Object Painter");
            objectPainter.transform.parent = g.transform;
            GUtilities.ResetTransform(objectPainter.transform, g.transform);
            objectPainter.transform.hideFlags = HideFlags.HideInInspector;
            GObjectPainter objectPainterComponent = objectPainter.AddComponent<GObjectPainter>();
            objectPainterComponent.GroupId = -1;
            objectPainterComponent.gameObject.AddComponent<GRotationRandomizeFilter>();
            objectPainterComponent.gameObject.AddComponent<GScaleRandomizeFilter>();

            GameObject assetExplorer = new GameObject("Asset Explorer");
            assetExplorer.transform.parent = g.transform;
            GUtilities.ResetTransform(assetExplorer.transform, g.transform);
            assetExplorer.transform.hideFlags = HideFlags.HideInInspector;
            assetExplorer.AddComponent<GAssetExplorer>();

            GameObject helpTool = new GameObject("Help");
            helpTool.transform.parent = g.transform;
            GUtilities.ResetTransform(helpTool.transform, g.transform);
            helpTool.transform.hideFlags = HideFlags.HideInInspector;
            helpTool.AddComponent<GHelpComponent>();

            Selection.activeGameObject = terrainGroup;
            Undo.RegisterCreatedObjectUndo(g, "Creating Terrain Tools");
        }

        [MenuItem("GameObject/3D Object/Polaris/Group Tool", false, -998)]
        public static void CreateGroupTool(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Terrain Group");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.hideFlags = HideFlags.HideInInspector;
            GTerrainGroup group = g.AddComponent<GTerrainGroup>();
            group.GroupId = -1;

            Selection.activeGameObject = g;
            Undo.RegisterCreatedObjectUndo(g, "Creating Terrain Group");
        }

        [MenuItem("GameObject/3D Object/Polaris/Paint Tools/Geometry - Texture Painter", false, -899)]
        public static void CreateTexturePainter(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Geometry & Texture Painter");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.hideFlags = HideFlags.HideInInspector;
            GTerrainTexturePainter painter = g.AddComponent<GTerrainTexturePainter>();
            painter.GroupId = -1;

            Selection.activeGameObject = g;
            Undo.RegisterCreatedObjectUndo(g, "Creating Texture Painter");
        }

        [MenuItem("GameObject/3D Object/Polaris/Paint Tools/Foliage Painter", false, -898)]
        public static void CreateFoliagePainter(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Foliage Painter");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.hideFlags = HideFlags.HideInInspector;
            GFoliagePainter painter = g.AddComponent<GFoliagePainter>();
            painter.GroupId = -1;
            painter.gameObject.AddComponent<GRotationRandomizeFilter>();
            painter.gameObject.AddComponent<GScaleRandomizeFilter>();

            Selection.activeGameObject = g;
            Undo.RegisterCreatedObjectUndo(g, "Creating Foliage Painter");
        }

        [MenuItem("GameObject/3D Object/Polaris/Paint Tools/Object Painter", false, -897)]
        public static void CreateObjectPainter(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Object Painter");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.hideFlags = HideFlags.HideInInspector;
            GObjectPainter painter = g.AddComponent<GObjectPainter>();
            painter.GroupId = -1;
            painter.gameObject.AddComponent<GRotationRandomizeFilter>();
            painter.gameObject.AddComponent<GScaleRandomizeFilter>();

            Selection.activeGameObject = g;
            Undo.RegisterCreatedObjectUndo(g, "Creating Object Painter");
        }

        [MenuItem("GameObject/3D Object/Polaris/Stamp Tools/Geometry Stamper", false, -799)]
        public static void CreateGeometryStamper(MenuCommand menuCmd)
        {
            GameObject geometryStamperGO = new GameObject("Geometry Stamper");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(geometryStamperGO, menuCmd.context as GameObject);
            geometryStamperGO.transform.localPosition = Vector3.zero;
            geometryStamperGO.transform.hideFlags = HideFlags.HideInInspector;
            GGeometryStamper geoStamper = geometryStamperGO.AddComponent<GGeometryStamper>();
            geoStamper.GroupId = -1;

            Selection.activeGameObject = geometryStamperGO;
            Undo.RegisterCreatedObjectUndo(geometryStamperGO, "Creating Geometry Stamper");
        }

        [MenuItem("GameObject/3D Object/Polaris/Stamp Tools/Texture Stamper", false, -798)]
        public static void CreateTextureStamper(MenuCommand menuCmd)
        {
            GameObject textureStamperGO = new GameObject("Texture Stamper");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(textureStamperGO, menuCmd.context as GameObject);
            textureStamperGO.transform.localPosition = Vector3.zero;
            textureStamperGO.transform.hideFlags = HideFlags.HideInInspector;
            GTextureStamper texStamper = textureStamperGO.AddComponent<GTextureStamper>();
            texStamper.GroupId = -1;

            Selection.activeGameObject = textureStamperGO;
            Undo.RegisterCreatedObjectUndo(textureStamperGO, "Creating Texture Stamper");
        }

        [MenuItem("GameObject/3D Object/Polaris/Stamp Tools/Foliage Stamper", false, -797)]
        public static void CreateFoliageStamper(MenuCommand menuCmd)
        {
            GameObject foliageStamperGO = new GameObject("Foliage Stamper");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(foliageStamperGO, menuCmd.context as GameObject);
            foliageStamperGO.transform.localPosition = Vector3.zero;
            foliageStamperGO.transform.hideFlags = HideFlags.HideInInspector;
            GFoliageStamper foliageStamper = foliageStamperGO.AddComponent<GFoliageStamper>();
            foliageStamper.GroupId = -1;

            Selection.activeGameObject = foliageStamperGO;
            Undo.RegisterCreatedObjectUndo(foliageStamperGO, "Creating Foliage Stampers");
        }

        [MenuItem("GameObject/3D Object/Polaris/Stamp Tools/Object Stamper", false, -796)]
        public static void CreateObjectStamper(MenuCommand menuCmd)
        {
            GameObject objectStamperGO = new GameObject("Object Stamper");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(objectStamperGO, menuCmd.context as GameObject);
            objectStamperGO.transform.localPosition = Vector3.zero;
            objectStamperGO.transform.hideFlags = HideFlags.HideInInspector;
            GObjectStamper objectStamper = objectStamperGO.AddComponent<GObjectStamper>();
            objectStamper.GroupId = -1;

            Selection.activeGameObject = objectStamperGO;
            Undo.RegisterCreatedObjectUndo(objectStamperGO, "Creating Object Stampers");
        }

        [MenuItem("GameObject/3D Object/Polaris/Erosion Simulator", false, -699)]
        public static void CreateErosionSimulator(MenuCommand menuCmd)
        {
            GameObject simulatorGO = new GameObject("Erosion Simulator");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(simulatorGO, menuCmd.context as GameObject);
            simulatorGO.transform.localPosition = Vector3.zero;
            simulatorGO.transform.localRotation = Quaternion.identity;
            simulatorGO.transform.localScale = Vector3.one * 100;
            GErosionSimulator simulator = simulatorGO.AddComponent<GErosionSimulator>();
            simulator.GroupId = -1;

            Selection.activeGameObject = simulatorGO;
            Undo.RegisterCreatedObjectUndo(simulatorGO, "Creating Erosion Simulator");
        }

        [MenuItem("GameObject/3D Object/Polaris/Spline Tool", false, -698)]
        public static void CreateSpline(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Spline");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.hideFlags = HideFlags.HideInInspector;
            GSplineCreator spline = g.AddComponent<GSplineCreator>();
            spline.GroupId = -1;

            Selection.activeGameObject = g;
            Undo.RegisterCreatedObjectUndo(g, "Creating Spline Tool");
        }

        [MenuItem("GameObject/3D Object/Polaris/Utilities/Tree Collider", false, -599)]
        public static void CreateTreeCollider(MenuCommand menuCmd)
        {
            GameObject g = new GameObject("Tree Collider");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(g, menuCmd.context as GameObject);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;

            GTreeCollider collider = g.AddComponent<GTreeCollider>();
            if (g.transform.parent != null)
            {
                GStylizedTerrain terrain = g.transform.parent.GetComponent<GStylizedTerrain>();
                collider.Terrain = terrain;
            }

            Undo.RegisterCreatedObjectUndo(g, "Creating Tree Collider");
            Selection.activeGameObject = g;
        }

        [MenuItem("GameObject/3D Object/Polaris/Utilities/Wind Zone", false, -598)]
        public static GWindZone CreateWindZone(MenuCommand menuCmd)
        {
            GameObject root = null;
            if (menuCmd != null && menuCmd.context != null)
            {
                root = menuCmd.context as GameObject;
            }

            GameObject windZoneGO = new GameObject("Wind Zone");
            GWindZone windZone = windZoneGO.AddComponent<GWindZone>();
            GameObjectUtility.SetParentAndAlign(windZoneGO, root);

            return windZone;
        }

        [MenuItem("GameObject/3D Object/Polaris/Utilities/Navigation Helper", false, -597)]
        public static void CreateNavigationHelper(MenuCommand menuCmd)
        {
            GameObject navHelperGO = new GameObject("Navigation Helper");
            if (menuCmd != null)
                GameObjectUtility.SetParentAndAlign(navHelperGO, menuCmd.context as GameObject);
            navHelperGO.transform.localPosition = Vector3.zero;
            navHelperGO.transform.hideFlags = HideFlags.HideInInspector;
            GNavigationHelper nav = navHelperGO.AddComponent<GNavigationHelper>();
            nav.GroupId = -1;

            Selection.activeGameObject = navHelperGO;
            Undo.RegisterCreatedObjectUndo(navHelperGO, "Creating Navigation Helper");
        }

        public static bool ValidateShowUnityTerrainConverter(MenuCommand menuCmd)
        {
            if (menuCmd == null)
                return false;
            GameObject go = menuCmd.context as GameObject;
            if (go == null)
                return false;
            Terrain[] terrains = go.GetComponentsInChildren<Terrain>();
            bool valid = false;
            for (int i = 0; i < terrains.Length; ++i)
            {
                if (terrains[i].terrainData != null)
                {
                    valid = true;
                    break;
                }
            }

            return valid;
        }

        [MenuItem("GameObject/3D Object/Polaris/Utilities/Convert From Unity Terrain", false, -596)]
        public static void ShowUnityTerrainConverter(MenuCommand menuCmd)
        {
            bool valid = ValidateShowUnityTerrainConverter(menuCmd);
            if (!valid)
            {
                Debug.Log("No Terrain with Terrain Data found. Select a game object with Terrain component.");
                return;
            }

            GUnityTerrainGroupConverterWindow window = GUnityTerrainGroupConverterWindow.ShowWindow();
            GameObject g = menuCmd.context as GameObject;
            window.Root = g;
        }

#if !VISTA
        [MenuItem("GameObject/3D Object/Polaris/Vista - Procedural Terrain Generator", false, 0)]
        public static void ShowVistaStorePage(MenuCommand menuCmd)
        {
            if (EditorUtility.DisplayDialog(
                "About Vista",
                "Vista is an advanced toolset for procedural terrain generation, which perfectly compatible with Polaris 3\n",
                "Show me more",
                "Close"))
            {
                Application.OpenURL(GAssetLink.VISTA_PRO);
            }
        }
#endif

        [MenuItem("Window/Polaris/Tools/Backup")]
        public static void ShowBackupWindow()
        {
            GBackupEditor.ShowWindow();
        }

        [MenuItem("Assets/Create/Polaris/Billboard Asset")]
        [MenuItem("Window/Polaris/Tools/Billboard Creator")]
        public static void ShowBillboardEditor()
        {
            GBillboardEditor.ShowWindow();
        }

        [MenuItem("Window/Polaris/Tools/Texture Creator")]
        public static void ShowTextureCreator()
        {
            GTextureEditorWindow.ShowWindow();
        }

        [MenuItem("Window/Polaris/Tools/Extensions", false, 1000000)]
        public static void ShowExtensionWindow()
        {
            GWizardWindow.ShowExtensionPage();
        }

        [MenuItem("Window/Polaris/Learning Resources/Online Manual")]
        public static void ShowOnlineUserGuide()
        {
            Application.OpenURL(GCommon.ONLINE_MANUAL);
        }

        [MenuItem("Window/Polaris/Learning Resources/Youtube Channel")]
        public static void ShowYoutubeChannel()
        {
            Application.OpenURL(GCommon.YOUTUBE_CHANNEL);
        }

        [MenuItem("Window/Polaris/Learning Resources/Facebook Page")]
        public static void ShowFacebookPage()
        {
            Application.OpenURL(GCommon.FACEBOOK_PAGE);
        }

        [MenuItem("Window/Polaris/Learning Resources/Help")]
        public static void ShowHelpEditor()
        {
            GHelpEditor.ShowWindow();
        }

        [MenuItem("Window/Polaris/Explore/Poseidon")]
        public static void ShowPoseidonLink()
        {
            GAssetExplorer.ShowPoseidonLink();
        }

        [MenuItem("Window/Polaris/Explore/Jupiter")]
        public static void ShowJupiterLink()
        {
            GAssetExplorer.ShowJupiterLink();
        }

        [MenuItem("Window/Polaris/Explore/CSharp Wizard")]
        public static void ShowCSharpWizardLink()
        {
            GAssetExplorer.ShowCSharpWizardLink();
        }

        [MenuItem("Window/Polaris/Explore/Mesh To File")]
        public static void ShowMeshToFileLink()
        {
            GAssetExplorer.ShowMeshToFileLink();
        }

        [MenuItem("Window/Polaris/Explore/Assets From Pinwheel")]
        public static void ShowAssetsFromPinwheel()
        {
            GAssetExplorer.ShowPinwheelAssets();
        }

        [MenuItem("Window/Polaris/Explore/Vegetation Assets")]
        public static void ShowStylizedVegetationLink()
        {
            GAssetExplorer.ShowVegetationLink();
        }

        [MenuItem("Window/Polaris/Explore/Rock - Props Assets")]
        public static void ShowStylizedRockPropsLink()
        {
            GAssetExplorer.ShowRockPropsLink();
        }

        [MenuItem("Window/Polaris/Explore/Character Assets")]
        public static void ShowStylizedCharacterLink()
        {
            GAssetExplorer.ShowCharacterLink();
        }

        [MenuItem("Window/Polaris/Contact Us")]
        public static void ShowContactPage()
        {
            Application.OpenURL(GCommon.CONTACT_PAGE);
        }

        [MenuItem("Window/Polaris/Write a Review")]
        public static void OpenStorePage()
        {
            Application.OpenURL(GCommon.STORE_PAGE);
        }

        //[MenuItem("~Pinwheel Internal/Clear Progress Bar")]
        //public static void ClearProgressBar()
        //{
        //    EditorUtility.ClearProgressBar();
        //}

    }
}
#endif
