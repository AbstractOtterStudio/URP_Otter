#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.GroupTool;
using Pinwheel.Griffin.PaintTool;
using Pinwheel.Griffin.SplineTool;
using Pinwheel.Griffin.StampTool;
using Pinwheel.Griffin.ErosionTool;
using Pinwheel.Griffin.TextureTool;

namespace Pinwheel.Griffin.Wizard
{
    public interface IWizardPage
    {
        string Path { get; }
        string Title { get; }
        //string Description => string.Empty;
        void OnTitleGUI(GWizardWindow hostWindow, Rect titleRect) { }
        void OnTitleRightGUILayout(GWizardWindow hostWindow) { }
        void OnGUI(GWizardWindow hostWindow) { }
        void OnPush(GWizardWindow hostWindow) { }
        bool OnPop(GWizardWindow hostWindow) { return true; }
    }

    public class MainPage : IWizardPage
    {
        public string Path => "Home";

        public string Title => "Polaris Wizard";

        private class PageGUI
        {
            public static readonly Texture2D PACKAGE_ICON = GEditorSkin.Instance.GetTexture("PackageIcon");

            public static readonly string HEADER_NEW_TERRAIN = "New Terrain";
            public static readonly GUIContent CREATE = new GUIContent("Create", "Add new terrains.");
            public static readonly GUIContent TERRAIN_MATERIAL = new GUIContent("Terrain Material", "Select default lighting & texturing mode.");
            public static readonly GUIContent DATA_LOCATION = new GUIContent("Data Location", "Select a folder for terrain data.");
            public static readonly GUIContent ADDITIONAL_SETTINGS = new GUIContent("Additional Settings", "Utility variables.");

            public static readonly string HEADER_ADD_TO_SCENE = "Add to Scene";
            public static readonly GUIContent GROUP_TOOL = new GUIContent("Group Tool", "Change settings of many terrains at once.");
            public static readonly GUIContent PAINTERS = new GUIContent("Painters", "Paint height, textures, vegetations and objects with brushes.");
            public static readonly GUIContent SPLINE = new GUIContent("Spline", "Create ramps, paint road texture, spawn & remove vegetations and objects.");
            public static readonly GUIContent STAMPERS = new GUIContent("Stampers", "Stamp height maps and spawn objects procedurally.");
            public static readonly GUIContent EROSION_SIMULATOR = new GUIContent("Erosion Simulator", "Apply thermal & hydraulic erosion on terrain geometry & textures.");
            public static readonly GUIContent WATER = new GUIContent("Water", PACKAGE_ICON, "Performant poly water shader with underwater & wet lens.");
            public static readonly GUIContent WIND = new GUIContent("Wind", "Adjust wind direction & intensity for grass waving.");
            public static readonly GUIContent PROC_TERRAIN = new GUIContent("Procedural Terrain", PACKAGE_ICON, "Generate your scene with graphs & biomes.");

            public static readonly string HEADER_SCENE_ENHANCEMENT = "Scene Enhancement";
            public static readonly GUIContent SKY = new GUIContent("Sky", PACKAGE_ICON, "Animated sky shader and day night cycle.");
            public static readonly GUIContent OUTLINE_FX = new GUIContent("Outline FX", PACKAGE_ICON, "Fullscreen post effect that adds subtle outline to objects.");
            public static readonly GUIContent VOLUMETRIC_LIGHTING = new GUIContent("Volumetric Lighting", PACKAGE_ICON, "Realistic sun shafts, halos and light beams.");

            public static readonly string HEADER_UTILITIES = "Utilities";
            public static readonly GUIContent CHANGE_MATERIAL = new GUIContent("Set Terrain Material", "Change lighting & texturing mode of terrains.");
            public static readonly GUIContent NAV_BAKING = new GUIContent("Nav Mesh Helper", "Create dummy objects as nav mesh obstacles.");
            public static readonly GUIContent EXTRACT_TERRAIN_TEXTURE = new GUIContent("Extract Textures", "Extract terrain textures and other maps to files.");
            public static readonly GUIContent EXTENSIONS = new GUIContent("Extensions", "Additional functionalities from other assets.");

            public static readonly Texture2D BELL_ICON = GEditorSkin.Instance.GetTexture("NotificationIcon");
            public static readonly Texture2D EMAIL_ICON = GEditorSkin.Instance.GetTexture("EmailIcon");
            public static readonly Texture2D DOC_ICON = GEditorSkin.Instance.GetTexture("DocumentationIcon");
            public static readonly GUIContent CONTACT = new GUIContent(EMAIL_ICON, "Contact us");
            public static readonly GUIContent DOCUMENTATION = new GUIContent(DOC_ICON, "Open Documentation");
        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            EditorGUILayout.LabelField(PageGUI.HEADER_NEW_TERRAIN, GStyles.H3);
            EditorGUILayout.BeginHorizontal();
            if (Tile(PageGUI.CREATE, DrawOverlay_CreateTerrain))
            {
                hostWindow.PushPage(new NewTerrainPage());
            }
            if (Tile(PageGUI.TERRAIN_MATERIAL))
            {
                hostWindow.PushPage(new MaterialPage());
            }
            if (Tile(PageGUI.DATA_LOCATION))
            {
                hostWindow.PushPage(new DataLocationPage());
            }
            if (Tile(PageGUI.ADDITIONAL_SETTINGS))
            {
                hostWindow.PushPage(new AdditionalSettingsPage());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(PageGUI.HEADER_ADD_TO_SCENE, GStyles.H3);
            EditorGUILayout.BeginHorizontal();
            if (Tile(PageGUI.GROUP_TOOL))
            {
                GTerrainGroup group = GWizard.CreateGroupTool();
                EditorGUIUtility.PingObject(group);
                Selection.activeGameObject = group.gameObject;
            }
            if (Tile(PageGUI.PAINTERS))
            {
                GTerrainTexturePainter texPainter = GWizard.CreateGeometryTexturePainter();
                GFoliagePainter foliagePainter = GWizard.CreateFoliagePainter();
                GObjectPainter objectPainter = GWizard.CreateObjectPainter();
                EditorGUIUtility.PingObject(texPainter);
                Selection.activeGameObject = texPainter.gameObject;
            }
            if (Tile(PageGUI.SPLINE))
            {
                GSplineCreator splineCreator = GWizard.CreateSplineTool();
                EditorGUIUtility.PingObject(splineCreator);
                Selection.activeGameObject = splineCreator.gameObject;
            }
            if (Tile(PageGUI.STAMPERS))
            {
                GGeometryStamper geoStamper = GWizard.CreateGeometryStamper();
                GTextureStamper texStamper = GWizard.CreateTextureStamper();
                GFoliageStamper foliageStamper = GWizard.CreateFoliageStamper();
                GObjectStamper objectStamper = GWizard.CreateObjectStamper();
                EditorGUIUtility.PingObject(geoStamper);
                Selection.activeGameObject = geoStamper.gameObject;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (Tile(PageGUI.EROSION_SIMULATOR))
            {
                GErosionSimulator erosionSim = GWizard.CreateErosionSimulator();
                EditorGUIUtility.PingObject(erosionSim);
                Selection.activeGameObject = erosionSim.gameObject;
            }
            if (Tile(PageGUI.WATER, null, DrawBackground_Poseidon))
            {
                hostWindow.PushPage(new PoseidonPage());
            }
            if (Tile(PageGUI.WIND))
            {
                GWindZone windZone = GWizard.CreateWindZone();
                EditorGUIUtility.PingObject(windZone);
                Selection.activeGameObject = windZone.gameObject;
            }
            if (Tile(PageGUI.PROC_TERRAIN, null, DrawBackground_Vista))
            {
                hostWindow.PushPage(new VistaPage());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(PageGUI.HEADER_SCENE_ENHANCEMENT, GStyles.H3);
            EditorGUILayout.BeginHorizontal();
            if (Tile(PageGUI.SKY, null, DrawBackground_Jupiter))
            {
                hostWindow.PushPage(new JupiterPage());
            }
            if (Tile(PageGUI.OUTLINE_FX, null, DrawBackground_Contour))
            {
                hostWindow.PushPage(new ContourPage());
            }
            if (Tile(PageGUI.VOLUMETRIC_LIGHTING, null, DrawBackground_Beam))
            {
                hostWindow.PushPage(new BeamPage());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(PageGUI.HEADER_UTILITIES, GStyles.H3);
            EditorGUILayout.BeginHorizontal();
            if (Tile(PageGUI.CHANGE_MATERIAL))
            {
                hostWindow.PushPage(new SetMaterialPage());
            }
            if (Tile(PageGUI.NAV_BAKING))
            {
                GNavigationHelper navHelper = GWizard.CreateNavHelper();
                EditorGUIUtility.PingObject(navHelper);
                Selection.activeGameObject = navHelper.gameObject;
            }
            if (Tile(PageGUI.EXTRACT_TERRAIN_TEXTURE))
            {
                GTextureEditorWindow.ShowWindow();
            }
            if (Tile(PageGUI.EXTENSIONS))
            {
                hostWindow.PushPage(new ExtensionPage());
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverlay_CreateTerrain(Rect tileRect)
        {
            float animF = 0;
            if (tileRect.Contains(Event.current.mousePosition))
            {
                System.DateTime time = System.DateTime.Now;
                float f = time.Ticks % 20000000;
                f = f / 20000000f;
                f = Mathf.Sin(f * Mathf.PI * 2) * 0.5f + 0.5f;
                animF = f;
            }

            Rect r = new Rect();
            r.size = new Vector2(30, 30);
            r.position = new Vector2(tileRect.max.x - r.size.x - 8, tileRect.max.y - r.size.y - 8 - animF * 6);

            float plusThickness = 3;
            Rect rPlusH = new Rect();
            rPlusH.size = new Vector2(r.width * 0.5f, plusThickness);
            rPlusH.center = r.center;

            Rect rPlusV = new Rect();
            rPlusV.size = new Vector2(plusThickness, r.height * 0.5f);
            rPlusV.center = r.center;

            Color plusColor = GEditorCommon.midGrey;
            EditorGUI.DrawRect(rPlusH, plusColor);
            EditorGUI.DrawRect(rPlusV, plusColor);
            Handles.BeginGUI();
            Handles.color = plusColor;
            Handles.DrawWireDisc(r.center, Vector3.forward, r.size.x * 0.5f);
            Handles.EndGUI();
            Handles.color = Color.white;
        }

        private void DrawTextureOverlayOnHover(Rect tileRect, string textureName)
        {
            if (tileRect.Contains(Event.current.mousePosition))
            {
                Texture2D overlay = GEditorSkin.Instance.GetTexture(textureName);
                if (overlay != null)
                {
                    GUI.DrawTexture(tileRect, overlay, ScaleMode.ScaleAndCrop);
                }
            }
        }

        private void DrawBackground_Poseidon(Rect tileRect)
        {
            DrawTextureOverlayOnHover(tileRect, "PoseidonTileHover");
        }

        private void DrawBackground_Jupiter(Rect tileRect)
        {
            DrawTextureOverlayOnHover(tileRect, "JupiterTileHover");
        }

        private void DrawBackground_Vista(Rect tileRect)
        {
            DrawTextureOverlayOnHover(tileRect, "VistaTileHover");
        }

        private void DrawBackground_Contour(Rect tileRect)
        {
            DrawTextureOverlayOnHover(tileRect, "ContourTileHover");
        }

        private void DrawBackground_Beam(Rect tileRect)
        {
            DrawTextureOverlayOnHover(tileRect, "BeamTileHover");
        }

        private class TileGUI
        {
            public static readonly Vector2 tileSize = new Vector2(140, 80);
            public static readonly Color borderColor = GEditorCommon.midGrey;
            public static readonly Color backgroundColor = GEditorCommon.darkGrey;
            public static readonly Color hoverColor = new Color(1, 1, 1, 0.05f);
            public static readonly Color clickedColor = new Color(0, 0, 0, 0.2f);

            public static Rect mouseTriggerRect = new Rect();
            public static bool isMouseTriggered = false;

            private static GUIStyle bodyStyle;
            public static GUIStyle BodyStyle
            {
                get
                {
                    if (bodyStyle == null)
                    {
                        bodyStyle = new GUIStyle();
                        bodyStyle.margin = new RectOffset(0, 8, 0, 8);
                        bodyStyle.padding = new RectOffset(8, 8, 8, 8);
                    }
                    return bodyStyle;
                }
            }
        }

        private bool Tile(GUIContent content, System.Action<Rect> overlayGUIDrawer = null, System.Action<Rect> backgroundDrawer = null)
        {
            Vector2 tileSize = TileGUI.tileSize;
            Rect r = EditorGUILayout.BeginVertical(TileGUI.BodyStyle, GUILayout.Width(tileSize.x), GUILayout.Height(tileSize.y));
            //if user left click on this tile, save it position and state for drawing highlight in latter GUI passes
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                r.Contains(Event.current.mousePosition))
            {
                TileGUI.mouseTriggerRect = r;
                TileGUI.isMouseTriggered = true;
            }

            //use release the left mouse button, reset 'active button' state
            if (Event.current.type == EventType.MouseUp &&
                Event.current.button == 0)
            {
                TileGUI.isMouseTriggered = false;
            }
            Handles.BeginGUI();
            GEditorCommon.DrawBodyBox(r, true);
            if (backgroundDrawer != null)
            {
                backgroundDrawer.Invoke(r);
            }
            EditorGUILayout.LabelField(content.text, GStyles.P1, GUILayout.Width(tileSize.x));
            EditorGUILayout.LabelField(content.tooltip, GStyles.P2, GUILayout.Width(tileSize.x));
            Handles.EndGUI();
            EditorGUILayout.EndVertical();

            if (content.image != null)
            {
                Rect iconRect = new Rect();
                iconRect.size = Vector2.one * EditorGUIUtility.singleLineHeight;
                iconRect.position = new(r.max.x - iconRect.width - 4, r.min.y + 4);
                GUI.color = GStyles.P2.normal.textColor;
                GUI.DrawTexture(iconRect, content.image);
                GUI.color = Color.white;
            }

            if (overlayGUIDrawer != null)
            {
                overlayGUIDrawer.Invoke(r);
            }

            //draw 'pressed' or 'hovered' highlight
            if (TileGUI.isMouseTriggered && TileGUI.mouseTriggerRect == r)
            {
                EditorGUI.DrawRect(r, TileGUI.clickedColor);
            }
            else if (!TileGUI.isMouseTriggered && r.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(r, TileGUI.hoverColor);
            }

            //user release the left mouse on the active button
            if (Event.current.type == EventType.MouseUp &&
                r.Contains(Event.current.mousePosition) &&
                r == TileGUI.mouseTriggerRect)
            {
                GUI.changed = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnTitleRightGUILayout(GWizardWindow hostWindow)
        {
            Rect contactRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
            GUI.contentColor = EditorStyles.label.normal.textColor;
            if (GUI.Button(contactRect, PageGUI.CONTACT, GEditorCommon.IconButton))
            {
                Application.OpenURL(GCommon.CONTACT_PAGE);
            }

            Rect documentationRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
            if (GUI.Button(documentationRect, PageGUI.DOCUMENTATION, GEditorCommon.IconButton))
            {
                Application.OpenURL(GCommon.ONLINE_MANUAL);
            }
            GUI.contentColor = Color.white;

            Rect notificationButtonRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
            if (GUI.Button(notificationButtonRect, PageGUI.BELL_ICON, GEditorCommon.IconButton))
            {
                PopupWindow.Show(notificationButtonRect, new ExplorePopup());
            }

            EditorGUILayout.GetControlRect(GUILayout.Width(4));//add some spacing to the right
        }

        private class ExplorePopup : PopupWindowContent
        {
            public override void OnOpen()
            {
                base.OnOpen();
                EditorApplication.update += editorWindow.Repaint;
            }

            public override void OnClose()
            {
                base.OnClose();
                EditorApplication.update -= editorWindow.Repaint;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(400, 600);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginVertical(GEditorCommon.WindowBodyStyle);
                GExploreTabDrawer.Draw();
                EditorGUILayout.EndVertical();
            }
        }
    }

    public class NewTerrainPage : IWizardPage
    {
        private class PageGUI
        {
            public static readonly GUIContent ORIGIN = new GUIContent("Origin", "Position of the first terrain in the grid.");
            public static readonly GUIContent TILE_SIZE = new GUIContent("Tile Size", "Size of each terrain tile in world space.");
            public static readonly GUIContent TILE_WIDTH = new GUIContent("Tile Width", "Width of each terrain tile in world space.");
            public static readonly GUIContent TILE_LENGTH = new GUIContent("Tile Length", "Length of each terrain tile in world space.");
            public static readonly GUIContent TILE_MAX_HEIGHT = new GUIContent("Tile Height", "Maximum height of each terrain tile in world space.");
            public static readonly GUIContent TILE_X = new GUIContent("Tile Count X", "Number of tiles along X-axis.");
            public static readonly GUIContent TILE_Z = new GUIContent("Tile Count Z", "Number of tiles along Z-axis.");
            public static readonly GUIContent CREATE_BTN = new GUIContent("Create");

            public static readonly GUIContent TERRAIN_MATERIAL = new GUIContent("Terrain Material");
            public static readonly GUIContent DATA_LOCATION = new GUIContent("Data Location");
            public static readonly GUIContent ADDITIONAL_SETTINGS = new GUIContent("Additional Settings");

            public static readonly Texture2D CONTEXT_ICON = GEditorSkin.Instance.GetTexture("ContextButtonIcon");

            public static readonly string TUT_KEY_SET_MATERIAL = "polaris-wizard-create-set-mat";
            public static readonly GUIContent TUT_SET_MATERIAL = new GUIContent("Set terrain material and other settings →");
        }

        public string Path => "Wizard/New Terrain";

        public string Title => "New Terrain";

        public void OnGUI(GWizardWindow hostWindow)
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
            float spacing = 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            int originalIndent = EditorGUI.indentLevel;

            #region pre-register spaces
            EditorGUILayout.GetControlRect(); //Tile Count X label
            EditorGUILayout.GetControlRect(); //Tile Count X field
            Rect rectTerrainGridBase = EditorGUILayout.GetControlRect(GUILayout.Height(lineHeight * 15));
            EditorGUILayout.GetControlRect(); //Origin field
            EditorGUILayout.GetControlRect(); //Tile Width field
            EditorGUILayout.GetControlRect(); //Tile Length field
            EditorGUILayout.GetControlRect(); //Tile Height field
            EditorGUILayout.Space();
            #endregion

            EditorGUI.BeginChangeCheck();
            Handles.BeginGUI();

            EditorGUIUtility.labelWidth = 70;
            EditorGUI.indentLevel = 0;

            #region Draw terrain grid & measurements
            Vector3 worldSize = new Vector3
            (
                settings.tileCountX * settings.tileSize.x,
                settings.tileSize.y,
                settings.tileCountZ * settings.tileSize.z
            );

            float worldAspect = worldSize.x / worldSize.z;
            float rectTerrainGridBaseSize = rectTerrainGridBase.height;
            Vector2 rectTerrainGridSize = new Vector2(rectTerrainGridBaseSize * worldAspect, rectTerrainGridBaseSize);
            Vector2 rectTerrainGridCenter = rectTerrainGridBase.center - new Vector2(0, lineHeight * 0);
            Rect rectTerrainGrid = new Rect()
            {
                size = rectTerrainGridSize,
                center = rectTerrainGridCenter
            };

            float rectTerrainGridScaleFactor = Mathf.Min(1, rectTerrainGridBase.width / rectTerrainGrid.width);
            rectTerrainGrid.size *= rectTerrainGridScaleFactor;
            rectTerrainGrid.center = rectTerrainGridCenter;

            Handles.DrawSolidRectangleWithOutline(rectTerrainGrid, Color.clear, Color.gray);
            for (int z = 0; z < settings.tileCountZ; ++z)
            {
                for (int x = 0; x < settings.tileCountX; ++x)
                {
                    Vector2 rectTileSize = new Vector2(rectTerrainGrid.width / settings.tileCountX, rectTerrainGrid.height / settings.tileCountZ);
                    Vector2 rectTilePosition = new Vector2(rectTerrainGrid.min.x + x * rectTileSize.x, rectTerrainGrid.min.y + z * rectTileSize.y);
                    Rect rectTile = new Rect()
                    {
                        size = rectTileSize,
                        position = rectTilePosition
                    };

                    Handles.DrawSolidRectangleWithOutline(rectTile, Color.clear, Color.gray);
                }
            }

            if (rectTerrainGrid.width > 50 || rectTerrainGrid.height > 50)
            {
                Handles.color = Handles.xAxisColor;
                Handles.DrawLine(
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y),
                    new Vector3(rectTerrainGrid.max.x, rectTerrainGrid.max.y),
                    1);
                Rect rectWorldWidth = new Rect()
                {
                    size = new Vector2(100, lineHeight),
                    center = new Vector2(rectTerrainGrid.center.x, rectTerrainGrid.max.y - lineHeight * 0.5f)
                };
                GUI.contentColor = Handles.xAxisColor * 1.2f;
                EditorGUI.LabelField(rectWorldWidth, $"{worldSize.x}m", GEditorCommon.CenteredBoldLabel);

                Handles.color = Handles.zAxisColor;
                Handles.DrawLine(
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.min.y),
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y),
                    1);
                Rect rectWorldLength = new Rect()
                {
                    size = new Vector2(100, lineHeight),
                    position = new Vector2(rectTerrainGrid.min.x + spacing, rectTerrainGrid.center.y - lineHeight * 0.5f)
                };
                GUI.contentColor = Handles.zAxisColor * 1.2f;
                EditorGUI.LabelField(rectWorldLength, $"{worldSize.z}m", GEditorCommon.BoldLabel);
            }
            #endregion

            #region Fields below terrain grid
            GUI.contentColor = Color.white;
            Rect rectOriginField = new Rect()
            {
                size = new Vector2(Mathf.Clamp(rectTerrainGrid.width, 250, 300), lineHeight),
                position = new Vector2(Mathf.Max(rectTerrainGridBase.min.x, rectTerrainGrid.min.x), rectTerrainGrid.max.y + spacing * 3)
            };
            Rect rectTileWidthField = new Rect()
            {
                size = rectOriginField.size,
                position = new Vector2(rectOriginField.min.x, rectOriginField.max.y + spacing)
            };
            Rect rectTileLengthField = new Rect()
            {
                size = rectOriginField.size,
                position = new Vector2(rectTileWidthField.min.x, rectTileWidthField.max.y + spacing)
            };
            Rect rectTileHeightField = new Rect()
            {
                size = rectOriginField.size,
                position = new Vector2(rectTileLengthField.min.x, rectTileLengthField.max.y + spacing)
            };

            EditorGUIUtility.wideMode = true;
            const string ORIGIN_FIELD_NAME = "OriginField";
            GUI.SetNextControlName(ORIGIN_FIELD_NAME);
            Vector3 origin = EditorGUI.Vector3Field(rectOriginField, PageGUI.ORIGIN, settings.origin);
            const string TILE_WIDTH_FIELD_NAME = "TileWidthField";
            GUI.SetNextControlName(TILE_WIDTH_FIELD_NAME);
            float tileWidth = EditorGUI.DelayedFloatField(rectTileWidthField, PageGUI.TILE_WIDTH, settings.tileSize.x);
            GUI.enabled = !settings.linkTileSize;
            const string TILE_LENGTH_FIELD_NAME = "TileLengthField";
            GUI.SetNextControlName(TILE_LENGTH_FIELD_NAME);
            float tileLength = EditorGUI.DelayedFloatField(rectTileLengthField, PageGUI.TILE_LENGTH, settings.tileSize.z);
            GUI.enabled = true;
            float tileHeight = EditorGUI.DelayedFloatField(rectTileHeightField, PageGUI.TILE_MAX_HEIGHT, settings.tileSize.y);

            if (string.Equals(ORIGIN_FIELD_NAME, GUI.GetNameOfFocusedControl()) ||
                rectOriginField.Contains(Event.current.mousePosition))
            {
                Handles.color = new Color(1, 1, 1, 0.7f);
                Handles.DrawSolidDisc(
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y),
                    Vector3.forward, 4);
            }

            if (string.Equals(TILE_WIDTH_FIELD_NAME, GUI.GetNameOfFocusedControl()) ||
                rectTileWidthField.Contains(Event.current.mousePosition))
            {
                Handles.color = new Color(1, 1, 1, 0.7f);
                Handles.DrawLine(
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y),
                    new Vector3(rectTerrainGrid.min.x + rectTerrainGrid.width / settings.tileCountX, rectTerrainGrid.max.y),
                    1);
            }

            if (string.Equals(TILE_LENGTH_FIELD_NAME, GUI.GetNameOfFocusedControl()) ||
              rectTileLengthField.Contains(Event.current.mousePosition))
            {
                Handles.color = new Color(1, 1, 1, 0.7f);
                Handles.DrawLine(
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y),
                    new Vector3(rectTerrainGrid.min.x, rectTerrainGrid.max.y - rectTerrainGrid.height / settings.tileCountZ),
                    1);
            }

            #region Link tile size
            Vector3[] linkTileSizeLine = new Vector3[]
            {
                    new Vector2(rectTileWidthField.max.x + spacing, rectTileWidthField.min.y),
                    new Vector2(rectTileWidthField.max.x + spacing*6, rectTileWidthField.min.y),
                    new Vector2(rectTileLengthField.max.x + spacing*6, rectTileLengthField.max.y),
                    new Vector2(rectTileLengthField.max.x + spacing, rectTileLengthField.max.y),
            };
            int[] linkTileSizeLineIndices = new int[] { 0, 1, 1, 2, 2, 3 };
            Handles.color = Color.gray;
            Handles.DrawLines(linkTileSizeLine, linkTileSizeLineIndices);

            Rect linkTileSizeButtonRect = new Rect()
            {
                size = Vector2.one * lineHeight,
                center = (linkTileSizeLine[1] + linkTileSizeLine[2]) * 0.5f
            };
            Texture2D linkIcon = null;
            if (settings.linkTileSize)
            {
                linkIcon = GEditorSkin.Instance.GetTexture("ChainIcon");
            }
            else
            {
                linkIcon = GEditorSkin.Instance.GetTexture("ChainBreakIcon");
            }
            bool linkTileSize = settings.linkTileSize;
            GUI.color = EditorStyles.label.normal.textColor;
            if (GUI.Button(linkTileSizeButtonRect, linkIcon, EditorStyles.iconButton))
            {
                linkTileSize = !linkTileSize;
            }
            if (linkTileSize)
            {
                tileLength = tileWidth;
            }
            GUI.color = Color.white;
            #endregion
            #endregion

            #region Tile Count fields
            float tileCountFieldWidth = 72;
            Rect rectTileCountZField = new Rect()
            {
                size = new Vector2(tileCountFieldWidth, lineHeight),
                position = new Vector2(Mathf.Max(rectTerrainGridBase.min.x, rectTerrainGrid.min.x), rectTerrainGrid.min.y - lineHeight - spacing * 2)
            };
            Rect rectTileCountZLabel = new Rect()
            {
                size = new Vector2(tileCountFieldWidth, lineHeight),
                position = new Vector2(rectTileCountZField.min.x, rectTileCountZField.min.y - lineHeight)
            };
            EditorGUI.LabelField(rectTileCountZLabel, PageGUI.TILE_Z);
            int tileCountZ = EditorGUI.DelayedIntField(rectTileCountZField, settings.tileCountZ);

            Rect rectTileCountXField = new Rect()
            {
                size = new Vector2(tileCountFieldWidth, lineHeight),
                position = new Vector2(Mathf.Min(rectTerrainGridBase.max.x - tileCountFieldWidth, rectTerrainGrid.max.x + spacing * 2), rectTerrainGrid.max.y - lineHeight)
            };
            Rect rectTileCountXLabel = new Rect()
            {
                size = new Vector2(tileCountFieldWidth, lineHeight),
                position = new Vector2(rectTileCountXField.min.x, rectTileCountXField.min.y - lineHeight)
            };
            EditorGUI.LabelField(rectTileCountXLabel, PageGUI.TILE_X);
            int tileCountX = EditorGUI.DelayedIntField(rectTileCountXField, settings.tileCountX);
            #endregion

            EditorGUIUtility.wideMode = false;
            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUI.indentLevel = originalIndent;
            Handles.EndGUI();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(GEditorSettings.Instance, "Change terrain wizard settings");
                EditorUtility.SetDirty(GEditorSettings.Instance);
                settings.origin = origin;
                settings.tileSize = new Vector3(
                    Mathf.Max(1, tileWidth),
                    Mathf.Max(1, tileHeight),
                    Mathf.Max(1, tileLength));
                settings.tileCountX = Mathf.Max(1, tileCountX);
                settings.tileCountZ = Mathf.Max(1, tileCountZ);
                settings.linkTileSize = linkTileSize;
            }
        }

        public void OnTitleRightGUILayout(GWizardWindow hostWindow)
        {
            if (!EditorPrefs.HasKey(PageGUI.TUT_KEY_SET_MATERIAL))
            {
                Vector2 tutLabelSize = GStyles.P1.CalcSize(PageGUI.TUT_SET_MATERIAL);
                EditorGUILayout.LabelField(PageGUI.TUT_SET_MATERIAL, GStyles.P1, GUILayout.Width(tutLabelSize.x));
            }

            Rect contextButtonRect = EditorGUILayout.GetControlRect(GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            GUI.contentColor = EditorStyles.label.normal.textColor;
            if (GUI.Button(contextButtonRect, PageGUI.CONTEXT_ICON, GEditorCommon.IconButton))
            {
                EditorPrefs.SetBool(PageGUI.TUT_KEY_SET_MATERIAL, true);
                GenericMenu menu = new GenericMenu();
                menu.AddItem(
                    PageGUI.TERRAIN_MATERIAL, false, () =>
                    {
                        PopupWindow.Show(contextButtonRect, new MaterialSelectorPopup());
                    });
                menu.AddItem(
                    PageGUI.DATA_LOCATION, false, () =>
                    {
                        PopupWindow.Show(contextButtonRect, new DataLocationPopup(hostWindow));
                    });
                menu.AddItem(
                    PageGUI.ADDITIONAL_SETTINGS, false, () =>
                    {
                        PopupWindow.Show(contextButtonRect, new AdditionalSettingsPopup(hostWindow));
                    });
                menu.ShowAsContext();
            }
            GUI.contentColor = Color.white;
            if (GUILayout.Button(PageGUI.CREATE_BTN, GUILayout.Width(100)))
            {
                GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
                GameObject environmentRoot = new GameObject("Low Poly Environment");
                environmentRoot.transform.position = settings.origin;

                GWizard.CreateTerrains(environmentRoot);
            }
            EditorGUILayout.GetControlRect(GUILayout.Width(4));
        }

        class MaterialSelectorPopup : PopupWindowContent
        {
            Vector2 scrollPos;

            public override Vector2 GetWindowSize()
            {
                return new Vector2(550, 600);
            }

            public override void OnGUI(Rect rect)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GEditorCommon.WindowBodyStyle);
                MaterialSelector.Draw();
                EditorGUILayout.EndScrollView();
            }
        }

        class DataLocationPopup : PopupWindowContent
        {
            DataLocationPage page;
            GWizardWindow wizardWindow;

            public DataLocationPopup(GWizardWindow wizardWindow)
            {
                this.page = new DataLocationPage();
                this.wizardWindow = wizardWindow;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(500, 54);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginVertical(GEditorCommon.WindowBodyStyle);
                EditorGUILayout.Space();
                page.OnGUI(wizardWindow);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
        }

        class AdditionalSettingsPopup : PopupWindowContent
        {
            AdditionalSettingsPage page;
            GWizardWindow wizardWindow;

            public AdditionalSettingsPopup(GWizardWindow wizardWindow)
            {
                this.page = new AdditionalSettingsPage();
                this.wizardWindow = wizardWindow;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(500, 54);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginVertical(GEditorCommon.WindowBodyStyle);
                EditorGUILayout.Space();
                page.OnGUI(wizardWindow);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
        }
    }

    public class MaterialPage : IWizardPage
    {
        private class PageGUI
        {
            public static readonly string DESCRIPTION = "Select the default material for new terrain. You can change the terrain material at anytime.";
            public static readonly GUIContent APPLY = new GUIContent("Apply");
        }

        public string Path => "Wizard/Material";
        public string Title => "Material";

        public void OnGUI(GWizardWindow hostWindow)
        {
            EditorGUILayout.LabelField(PageGUI.DESCRIPTION);
            MaterialSelector.Draw();
        }
    }

    public class SetMaterialPage : IWizardPage
    {
        public string Path => "Wizard/Set Material";
        public string Title => "Set Material";
        public bool bulkMode = false;

        private class PageGUI
        {
            public static readonly string DESCRIPTION = "Select new lighting & texturing mode for your terrain.";
            public static readonly string HEADER_TARGET = "Target";
            public static readonly GUIContent GROUP_ID = new GUIContent("Group Id", "Id of the terrain group to change the material");
            public static readonly GUIContent TERRAIN = new GUIContent("Terrain", "The terrain to change its material");

            public static readonly GUIContent APPLY_BTN = new GUIContent("Apply");
        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
            EditorGUILayout.LabelField(PageGUI.DESCRIPTION);
            EditorGUI.BeginChangeCheck();
            GEditorCommon.Header(PageGUI.HEADER_TARGET);
            int groupId = settings.setShaderGroupId;
            GStylizedTerrain terrain = settings.setShaderTerrain;
            if (bulkMode)
            {
                groupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption(PageGUI.GROUP_ID, settings.setShaderGroupId);
            }
            else
            {
                terrain = EditorGUILayout.ObjectField(PageGUI.TERRAIN, settings.setShaderTerrain, typeof(GStylizedTerrain), true) as GStylizedTerrain;
            }
            MaterialSelector.Draw();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(GEditorSettings.Instance, "Change Wizard settings");
                EditorUtility.SetDirty(GEditorSettings.Instance);
                settings.setShaderGroupId = groupId;
                settings.setShaderTerrain = terrain;
            }
        }

        public void OnTitleRightGUILayout(GWizardWindow hostWindow)
        {
            if (GUILayout.Button(PageGUI.APPLY_BTN, GUILayout.Width(100)))
            {
                GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
                if (bulkMode)
                {
                    GWizard.SetShader(settings.setShaderGroupId);
                }
                else
                {
                    GWizard.SetShader(settings.setShaderTerrain);
                }
            }
        }
    }

    public static class MaterialSelector
    {
        private static class PageGUI
        {
            public static readonly string HEADER_LIGHTING_MODEL = "Lighting Model";
            public static readonly string PHYSICAL_BASED = "Physical Based";
            public static readonly string PHYSICAL_BASED_DESC = "Current-gen lighting method with physical properties.";
            public static readonly Texture2D PHYSICAL_BASED_IMAGE = GEditorSkin.Instance.GetTexture("PhysicalBasedThumbnail");

            public static readonly string LAMBERT = "Lambert";
            public static readonly string LAMBERT_DESC = "Simple diffuse lighting, mobile friendly.";
            public static readonly Texture2D LAMBERT_IMAGE = GEditorSkin.Instance.GetTexture("LambertThumbnail");

            public static readonly string BLINN_PHONG = "Blinn-Phong";
            public static readonly string BLINN_PHONG_DESC = "Simple specular lighting, mobile friendly.";
            public static readonly Texture2D BLINN_PHONG_IMAGE = GEditorSkin.Instance.GetTexture("BlinnPhongThumbnail");

            public static readonly string HEADER_TEXTURING_MODEL = "Texturing Model";
            public static readonly string SPLATS4 = "4 Splats";
            public static readonly string SPLATS4_DESC = "4 texture layers without normal maps blend together using painted weight. The 5th layer and up will appear black.";
            public static readonly Texture2D SPLATS4_IMAGE = GEditorSkin.Instance.GetTexture("Splats4Thumbnail");

            public static readonly string SPLATS4_NORMALS4 = "4 Splats with Normal Maps";
            public static readonly string SPLATS4_NORMALS4_DESC = "4 texture layers with normal maps blend together using painted weight. The 5th layer and up will appear black.";
            public static readonly Texture2D SPLATS4_NORMALS4_IMAGE = GEditorSkin.Instance.GetTexture("Splats4Normals4Thumbnail");

            public static readonly string SPLATS8 = "8 Splats";
            public static readonly string SPLATS8_DESC = "8 texture layers without normal maps blend together using painted weight. The 9th layer and up will appear black.";
            public static readonly Texture2D SPLATS8_IMAGE = GEditorSkin.Instance.GetTexture("Splats8Thumbnail");

            public static readonly string COLOR_MAP = "Color Map";
            public static readonly string COLOR_MAP_DESC = "A color map and a metallic/smoothness map cover the whole terrain.";
            public static readonly Texture2D COLOR_MAP_IMAGE = GEditorSkin.Instance.GetTexture("ColorMapThumbnail");

            public static readonly string GL = "Gradient Lookup";
            public static readonly string GL_DESC = "Procedural coloring with color by height and color by normal vectors, encoded in gradients, plus a color map for overlaying detail.";
            public static readonly Texture2D GL_IMAGE = GEditorSkin.Instance.GetTexture("GradientLookupThumbnail");

            public static readonly string VERTEX_COLOR = "Vertex Color";
            public static readonly string VERTEX_COLOR_DESC = "Colors are baked into the terrain mesh. Cheap to render but no live preview.";
            public static readonly Texture2D VERTEX_COLOR_IMAGE = GEditorSkin.Instance.GetTexture("VertexColorThumbnail");

        }

        public static void Draw()
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;

            EditorGUI.BeginChangeCheck();
            GEditorCommon.Header(PageGUI.HEADER_LIGHTING_MODEL);
            if (GCommon.CurrentRenderPipeline == GRenderPipelineType.Universal)
            {
                settings.lightingModel = GLightingModel.PBR;
            }
            GLightingModel lightingModel = settings.lightingModel;

            EditorGUILayout.BeginHorizontal();
            if (Card(PageGUI.PHYSICAL_BASED_IMAGE, PageGUI.PHYSICAL_BASED, PageGUI.PHYSICAL_BASED_DESC, lightingModel == GLightingModel.PBR))
            {
                lightingModel = GLightingModel.PBR;
            }
            if (GCommon.CurrentRenderPipeline != GRenderPipelineType.Universal)
            {
                if (Card(PageGUI.LAMBERT_IMAGE, PageGUI.LAMBERT, PageGUI.LAMBERT_DESC, lightingModel == GLightingModel.Lambert))
                {
                    lightingModel = GLightingModel.Lambert;
                }
                if (Card(PageGUI.BLINN_PHONG_IMAGE, PageGUI.BLINN_PHONG, PageGUI.BLINN_PHONG_DESC, lightingModel == GLightingModel.BlinnPhong))
                {
                    lightingModel = GLightingModel.BlinnPhong;
                }
            }
            EditorGUILayout.EndHorizontal();

            GEditorCommon.Space();

            GEditorCommon.Header(PageGUI.HEADER_TEXTURING_MODEL);
            GTexturingModel texturingModel = settings.texturingModel;
            GSplatsModel splatsModel = settings.splatsModel;
            EditorGUILayout.BeginHorizontal();
            if (Card(PageGUI.SPLATS4_IMAGE, PageGUI.SPLATS4, PageGUI.SPLATS4_DESC, texturingModel == GTexturingModel.Splat && splatsModel == GSplatsModel.Splats4, true))
            {
                texturingModel = GTexturingModel.Splat;
                splatsModel = GSplatsModel.Splats4;
            }
            if (Card(PageGUI.SPLATS4_NORMALS4_IMAGE, PageGUI.SPLATS4_NORMALS4, PageGUI.SPLATS4_NORMALS4_DESC, texturingModel == GTexturingModel.Splat && splatsModel == GSplatsModel.Splats4Normals4, true))
            {
                texturingModel = GTexturingModel.Splat;
                splatsModel = GSplatsModel.Splats4Normals4;
            }
            if (Card(PageGUI.SPLATS8_IMAGE, PageGUI.SPLATS8, PageGUI.SPLATS8_DESC, texturingModel == GTexturingModel.Splat && splatsModel == GSplatsModel.Splats8, true))
            {
                texturingModel = GTexturingModel.Splat;
                splatsModel = GSplatsModel.Splats8;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (Card(PageGUI.COLOR_MAP_IMAGE, PageGUI.COLOR_MAP, PageGUI.COLOR_MAP_DESC, texturingModel == GTexturingModel.ColorMap, true))
            {
                texturingModel = GTexturingModel.ColorMap;
            }
            if (Card(PageGUI.GL_IMAGE, PageGUI.GL, PageGUI.GL_DESC, texturingModel == GTexturingModel.GradientLookup, true))
            {
                texturingModel = GTexturingModel.GradientLookup;
            }
            if (Card(PageGUI.VERTEX_COLOR_IMAGE, PageGUI.VERTEX_COLOR, PageGUI.VERTEX_COLOR_DESC, texturingModel == GTexturingModel.VertexColor, true))
            {
                texturingModel = GTexturingModel.VertexColor;
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(GEditorSettings.Instance, "Change wizard settings");
                EditorUtility.SetDirty(GEditorSettings.Instance);
                settings.lightingModel = lightingModel;
                settings.texturingModel = texturingModel;
                settings.splatsModel = splatsModel;
            }
        }

        private static class CardGUI
        {
            private static GUIStyle cardStyle;
            public static GUIStyle CardStyle
            {
                get
                {
                    if (cardStyle == null)
                    {
                        cardStyle = new GUIStyle();
                        cardStyle.margin = new RectOffset(0, 8, 0, 8);
                    }
                    return cardStyle;
                }
            }

            private static GUIStyle cardImageStyle;
            public static GUIStyle CardImageStyle
            {
                get
                {
                    if (cardImageStyle == null)
                    {
                        cardImageStyle = new GUIStyle();
                        cardImageStyle.margin = new RectOffset(1, 1, 0, 0);
                    }
                    return cardImageStyle;
                }
            }

            private static GUIStyle expandButtonStyle;
            public static GUIStyle ExpandButtonStyle
            {
                get
                {
                    if (expandButtonStyle == null)
                    {
                        expandButtonStyle = new GUIStyle();
                        expandButtonStyle.padding = new RectOffset(4, 4, 4, 4);
                    }
                    return expandButtonStyle;
                }
            }

            public static readonly Texture2D EXPAND_ICON = GEditorSkin.Instance.GetTexture("ExpandIcon");
        }

        private static bool Card(Texture2D image, string title, string description, bool selected, bool allowImageExpand = false)
        {
            Vector2 cardSize = new Vector2(160, 200);
            Rect r = EditorGUILayout.BeginVertical(CardGUI.CardStyle, GUILayout.Width(cardSize.x), GUILayout.Height(cardSize.y));
            Handles.BeginGUI();
            GEditorCommon.DrawBodyBox(r);
            if (selected)
            {
                GEditorCommon.DrawOutlineBox(r, GEditorCommon.selectedItemColor);
            }

            Rect imageRect = EditorGUILayout.GetControlRect(false, 100, CardGUI.CardImageStyle);
            if (image != null)
            {
                GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit, true);
            }
            else
            {
                EditorGUI.DrawRect(imageRect, Color.black);
            }

            if (allowImageExpand && image != null && imageRect.Contains(Event.current.mousePosition))
            {
                Vector2 expandImgButtonSize = new Vector2(24, 24);
                Vector2 expandImgButtonPosition = new Vector2(imageRect.max.x - expandImgButtonSize.x, imageRect.min.y);
                Rect expandImageButtonRect = new Rect()
                {
                    size = expandImgButtonSize,
                    position = expandImgButtonPosition
                };
                if (GUI.Button(expandImageButtonRect, CardGUI.EXPAND_ICON, CardGUI.ExpandButtonStyle))
                {
                    PopupWindow.Show(expandImageButtonRect, new GImageZoomPopup(image));
                }
            }

            GEditorCommon.Space();
            EditorGUILayout.LabelField(title, GEditorCommon.CenteredLabel, GUILayout.Width(cardSize.x));
            EditorGUILayout.LabelField(description, GEditorCommon.CenteredGrayMiniLabel, GUILayout.Width(cardSize.x));

            Handles.EndGUI();

            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition))
            {
                GUI.changed = true;
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    public class DataLocationPage : IWizardPage
    {
        public string Path => "Wizard/Data Location";

        public string Title => "Data Location";

        private class PageGUI
        {
            public static readonly GUIContent INSTRUCTION = new GUIContent("Select a folder to store your new terrain data assets, terrain meshes and materials.");
            public static readonly GUIContent DIRECTORY = new GUIContent("Directory", "Where to store created terrain data. A sub-folder of Assets/ is recommended.");
        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
            EditorGUILayout.LabelField(PageGUI.INSTRUCTION);
            EditorGUI.BeginChangeCheck();
            string dir = settings.dataDirectory;
            GEditorCommon.BrowseFolder(PageGUI.DIRECTORY, ref dir, true);
            if (string.IsNullOrEmpty(dir))
            {
                dir = "Assets/";
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(GEditorSettings.Instance, "Change wizard settings");
                EditorUtility.SetDirty(GEditorSettings.Instance);
                settings.dataDirectory = dir;
            }
        }
    }

    public class AdditionalSettingsPage : IWizardPage
    {
        public string Path => "Wizard/Additional Settings";

        public string Title => "Additional Settings";

        private class PageGUI
        {
            public static readonly GUIContent NAME_PREFIX = new GUIContent("Name Prefix", "The beginning of each terrain's name. Useful for some level streaming system.");
            public static readonly GUIContent GROUP_ID = new GUIContent("Group Id", "An integer for grouping and connecting adjacent terrain tiles.");
        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
            EditorGUI.BeginChangeCheck();
            string namePrefix = EditorGUILayout.TextField(PageGUI.NAME_PREFIX, settings.terrainNamePrefix);
            int groupId = EditorGUILayout.IntField(PageGUI.GROUP_ID, settings.groupId);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(GEditorSettings.Instance, "Change wizard settings");
                EditorUtility.SetDirty(GEditorSettings.Instance);
                settings.terrainNamePrefix = namePrefix;
                settings.groupId = groupId;
            }
        }
    }

    public class ExtensionPage : IWizardPage
    {
        public string Path => "Wizard/Extension";

        public string Title => "Extensions";

        private class PageGUI
        {

        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            GExtensionTabDrawer.Draw();
        }
    }

    public abstract class AssetAdPage : IWizardPage
    {
        public abstract string Path { get; }
        public abstract string Title { get; }
        public abstract Texture2D BANNER { get; }
        public abstract string HEADING { get; }
        public abstract string SUBHEADING { get; }
        public abstract string CTA { get; }
        public abstract string CTA_LINK { get; }

        private static GUIStyle bannerStyle;
        private static GUIStyle BannerStyle
        {
            get
            {
                if (bannerStyle == null)
                {
                    bannerStyle = new GUIStyle();
                    bannerStyle.padding = new RectOffset(24, 0, 0, 0);
                }
                return bannerStyle;
            }
        }

        public void OnGUI(GWizardWindow hostWindow)
        {
            Rect bannerRect = EditorGUILayout.BeginVertical(BannerStyle);
            GUI.DrawTexture(bannerRect, BANNER, ScaleMode.ScaleAndCrop);
            EditorGUILayout.GetControlRect(GUILayout.Height(64));
            EditorGUILayout.LabelField(HEADING, GStyles.H1, GUILayout.Width(400));
            EditorGUILayout.LabelField(SUBHEADING, GStyles.P1, GUILayout.Width(400));
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(CTA, GUILayout.Width(150), GUILayout.Height(24)))
            {
                Application.OpenURL(CTA_LINK);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.GetControlRect(GUILayout.Height(64));
            EditorGUILayout.EndVertical();
        }
    }

    public class PoseidonPage : AssetAdPage
    {
        public override string Path => "Wizard/Poseidon";
        public override string Title => "Poseidon";

        public override Texture2D BANNER => PageGUI.BANNER;
        public override string HEADING => PageGUI.HEADING;
        public override string SUBHEADING => PageGUI.SUBHEADING;
        public override string CTA => PageGUI.CTA;
        public override string CTA_LINK => PageGUI.CTA_LINK;

        private class PageGUI
        {
            public static readonly Texture2D BANNER = GEditorSkin.Instance.GetTexture("PoseidonBanner");
            public static readonly string HEADING = "Low Poly Water";
            public static readonly string SUBHEADING = "Breath live to your scene with Poseidon, GPU animated water shader and underwater, wet lens post FX.";
            public static readonly string CTA = "View on Asset Store";
            public static readonly string CTA_LINK = GAssetLink.POSEIDON;
        }
    }

    public class JupiterPage : AssetAdPage
    {
        public override string Path => "Wizard/Jupiter";
        public override string Title => "Jupiter";

        public override Texture2D BANNER => PageGUI.BANNER;
        public override string HEADING => PageGUI.HEADING;
        public override string SUBHEADING => PageGUI.SUBHEADING;
        public override string CTA => PageGUI.CTA;
        public override string CTA_LINK => PageGUI.CTA_LINK;

        private class PageGUI
        {
            public static readonly Texture2D BANNER = GEditorSkin.Instance.GetTexture("JupiterBanner");
            public static readonly string HEADING = "Animated Sky";
            public static readonly string SUBHEADING = "Uplift scene ambient with Jupiter, a procedural sky shader and day night cycle system.";
            public static readonly string CTA = "View on Asset Store";
            public static readonly string CTA_LINK = GAssetLink.JUPITER;
        }
    }

    public class VistaPage : AssetAdPage
    {
        public override string Path => "Wizard/Vista";
        public override string Title => "Vista";

        public override Texture2D BANNER => PageGUI.BANNER;
        public override string HEADING => PageGUI.HEADING;
        public override string SUBHEADING => PageGUI.SUBHEADING;
        public override string CTA => PageGUI.CTA;
        public override string CTA_LINK => PageGUI.CTA_LINK;

        private class PageGUI
        {
            public static readonly Texture2D BANNER = GEditorSkin.Instance.GetTexture("VistaBanner");
            public static readonly string HEADING = "Procedural Terrain Generator";
            public static readonly string SUBHEADING = "Generate your terrain with Vista, a powerful graph tool with multi-biomes workflow.";
            public static readonly string CTA = "View on Asset Store";
            public static readonly string CTA_LINK = GAssetLink.VISTA_PRO;
        }
    }

    public class ContourPage : AssetAdPage
    {
        public override string Path => "Wizard/Contour";
        public override string Title => "Contour";

        public override Texture2D BANNER => PageGUI.BANNER;
        public override string HEADING => PageGUI.HEADING;
        public override string SUBHEADING => PageGUI.SUBHEADING;
        public override string CTA => PageGUI.CTA;
        public override string CTA_LINK => PageGUI.CTA_LINK;

        private class PageGUI
        {
            public static readonly Texture2D BANNER = GEditorSkin.Instance.GetTexture("ContourBanner");
            public static readonly string HEADING = "Edge Detection & Outline";
            public static readonly string SUBHEADING = "Make your visual aesthetic with Contour, a fullscreen post FX that adds subtle outline around objects.";
            public static readonly string CTA = "View on Asset Store";
            public static readonly string CTA_LINK = GAssetLink.CONTOUR;
        }
    }

    public class BeamPage : AssetAdPage
    {
        public override string Path => "Wizard/Beam";
        public override string Title => "Beam";

        public override Texture2D BANNER => PageGUI.BANNER;
        public override string HEADING => PageGUI.HEADING;
        public override string SUBHEADING => PageGUI.SUBHEADING;
        public override string CTA => PageGUI.CTA;
        public override string CTA_LINK => PageGUI.CTA_LINK;

        private class PageGUI
        {
            public static readonly Texture2D BANNER = GEditorSkin.Instance.GetTexture("BeamBanner");
            public static readonly string HEADING = "Volumetric Lighting & Fog";
            public static readonly string SUBHEADING = "Next level lighting with volumetric sun shafts, halos and light beams.";
            public static readonly string CTA = "View on Asset Store";
            public static readonly string CTA_LINK = GAssetLink.BEAM;
        }
    }
}

#endif