using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace FluffyGroomingTool {
    public class FluffyStartScreen : EditorWindow {
        public static readonly string DOCUMENTATION_LINK = "https://danielzeller427.gitbook.io/fluffy-grooming-tool/";
        public static readonly string DISCORD_LINK = "https://discord.gg/9ERpaa4EKg";
        private static readonly string IS_FIRST_EVER_LAUNCH = "IS_FIRST_EVER_LAUNCH";

        private const string hasAnyMaterialBeenImported = "HasAnyMatBeenImported";
        private const string hasAnyMaterialBeenImportedV2 = "HasAnyMatBeenImportedv2";
        private const string builtInShaders = "BuiltInRendererShaders";
        private const string urpShaders = "URPShaders";
        private const string hdrpShaders = "HDRPShaders";
        private const string hqLineShader = "HqLineRendererShader";
        private const string moveKey = "MoveKey";

        private readonly BannerImage bannerImage = new BannerImage();
        private readonly EditorDeltaTime deltaTime = new EditorDeltaTime();
        private static DelayedAction delayedAction;

        private readonly ImportantButton importSamples = new ImportantButton() {
            positionRect = new Rect(162, 460, 325, 75),
            resource = "import_textures_button",
            clickAction = delegate {
                doImportSamples();
                pingExampleScene();
            }
        };

        private static void pingExampleScene() {
            delayedAction = new DelayedAction(0.5f, delegate { doPingScene(); });
        }

        private static void doPingScene() {
            var package = AssetDatabase.FindAssets("FluffyExample1", null);
            var scene = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(package[1]), typeof(Object));
            Selection.activeObject = scene;
            EditorGUIUtility.PingObject(scene);
        }

        [MenuItem("Tools/Fluffy Grooming Tool/Import Examples")]
        private static void doImportSamples() {
            var isHdrp = GraphicsSettings.currentRenderPipeline &&
                         GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition");
            var isUrp = GraphicsSettings.currentRenderPipeline &&
                        GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("Universal");

            if (isHdrp) {
                importPackage("HDRPExample", "Assets/FluffyGroomingTool/Examples/HDRP");
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
                importPackage("HDRPAlembicExample", "Assets/FluffyGroomingTool/Examples/HDRPAlembicExample");
#endif
            } else if (isUrp) {
                importPackage("URPExample", "Assets/FluffyGroomingTool/Examples/URP");
            } else {
                importPackage("BuiltInRendererExample", "Assets/FluffyGroomingTool/Examples/BuiltIn");
            }
        }

        private RippleButton discordButton = new RippleButton() {
            positionRect = new Rect(162, 570, 120, 45),
            resource = "outline_button",
            text = "Discord",
            clickAction = delegate { Application.OpenURL(DISCORD_LINK); }
        };

        [MenuItem("Tools/Fluffy Grooming Tool/Documentation")]
        private static void openDocumentation() {
            Application.OpenURL(DOCUMENTATION_LINK);
        }

        private RippleButton documentation = new RippleButton() {
            positionRect = new Rect(315, 570, 170, 45),
            resource = "outline_button",
            text = "Documentation",
            clickAction = openDocumentation
        };

        private RippleButton urp = new RippleButton() {
            positionRect = new Rect(281, 670, 86, 45),
            resource = "outline_button",
            text = "URP",
            clickAction = delegate { importPackage(urpShaders, "Assets/FluffyGroomingTool/Shaders/URP"); }
        };

        private RippleButton builtIn = new RippleButton() {
            positionRect = new Rect(162, 670, 86, 45),
            resource = "outline_button",
            text = "Built In",
            clickAction = delegate { importPackage(builtInShaders, "Assets/FluffyGroomingTool/Shaders/BuiltInRenderer"); }
        };

        private RippleButton hdrp = new RippleButton() {
            positionRect = new Rect(399, 670, 86, 45),
            resource = "outline_button",
            text = "HDRP",
            clickAction = delegate {
                importPackage(hdrpShaders, "Assets/FluffyGroomingTool/Shaders/HDRP");
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
                importPackage(hqLineShader, "Assets/FluffyGroomingTool/Shaders/HDRP");
#endif
            }
        };

        private static void importPackage(string name, string unpackedFolder) {
            var package = AssetDatabase.FindAssets(name, null);
            if (package.Length > 0) {
                var guidToAssetPath = AssetDatabase.GUIDToAssetPath(package[0]);
                AssetDatabase.ImportPackage(guidToAssetPath, false);

                var targetDestination = guidToAssetPath.Substring(0, guidToAssetPath.LastIndexOf("/", StringComparison.Ordinal));
                var lastSlashIndex = unpackedFolder.LastIndexOf("/", StringComparison.Ordinal);
                var lastFolderName = unpackedFolder.Substring(lastSlashIndex, unpackedFolder.Length - lastSlashIndex);
                var destination = $"{targetDestination}{lastFolderName}";

                if (!guidToAssetPath.Contains("Assets/FluffyGroomingTool/")) {
                    PlayerPrefs.SetString(moveKey, unpackedFolder + "-" + destination);
                    AssetDatabase.onImportPackageItemsCompleted += completePendingPackageMove;
                }
            }

            PerProjectPreferences.setInt(name, 1);
            updateButtonsEnabledState();
        }

        public static void completePendingPackageMove(string[] _) {
            if (PlayerPrefs.GetString(moveKey).Length > 0) {
                var sourceAndDestination = PlayerPrefs.GetString(moveKey);
                PlayerPrefs.DeleteKey(moveKey);
                var split = sourceAndDestination.Split('-');
                var unpackedFolder = split[0];
                var destination = split[1];
                if (AssetDatabase.IsValidFolder(unpackedFolder)) {
                    FileUtil.DeleteFileOrDirectory(destination);
                    FileUtil.CopyFileOrDirectory(unpackedFolder, destination);
                    FileUtil.DeleteFileOrDirectory(unpackedFolder + ".meta");
                    if (!destination.Contains("Assets/FluffyGroomingTool/")) {
                        FileUtil.DeleteFileOrDirectory("Assets/FluffyGroomingTool");
                        FileUtil.DeleteFileOrDirectory("Assets/FluffyGroomingTool.meta");
                    }
                }

                AssetDatabase.Refresh();
            }
        }

        private GUIStyle defaultTextStyle;
        private GUIStyle boldTextStyle;
        private GUIStyle toggleStyle;

        private bool showAtStartup;
        private static int hasBuiltInShaderPackBeenImported;
        private static int hasUrpShaderPackBeenImported;
        private static int hasHdrpShaderPackBeenImported;

        private void OnEnable() {
            showAtStartup = PerProjectPreferences.hasKey(FurStartScreenLauncher.SHOWS_START_SCREEN);
            updateButtonsEnabledState();
            importDefaultMaterials();
        }

        private static void updateButtonsEnabledState() {
            hasBuiltInShaderPackBeenImported = PerProjectPreferences.getInt(builtInShaders, 0);
            hasUrpShaderPackBeenImported = PerProjectPreferences.getInt(urpShaders, 0);
            hasHdrpShaderPackBeenImported = PerProjectPreferences.getInt(hdrpShaders, 0);
        }

        private static void importDefaultMaterials() {
            if (!PerProjectPreferences.hasKey(hasAnyMaterialBeenImported)) {
                Debug.Log("Auto imported package");
                PerProjectPreferences.setInt(hasAnyMaterialBeenImported, 1);
                var isHDRP = GraphicsSettings.currentRenderPipeline &&
                             GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition");
                var isURP = GraphicsSettings.currentRenderPipeline &&
                            GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("Universal");
                if (isHDRP) {
                    importPackage(hdrpShaders, "Assets/FluffyGroomingTool/Shaders/HDRP");
                } else if (isURP) {
                    importPackage(urpShaders, "Assets/FluffyGroomingTool/Shaders/URP");
                } else {
                    importPackage(builtInShaders, "Assets/FluffyGroomingTool/Shaders/BuiltInRenderer");
                }

            }

            if (!PerProjectPreferences.hasKey(hasAnyMaterialBeenImportedV2)) {
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
                    importPackage(hqLineShader, "Assets/FluffyGroomingTool/Shaders/HDRP");
#endif
                PerProjectPreferences.setInt(hasAnyMaterialBeenImportedV2, 1);
            }
        }

        [MenuItem("Tools/Fluffy Grooming Tool/Show Start Screen", false, 1999)]
        public static void init() {
            var window = (FluffyStartScreen)GetWindow(typeof(FluffyStartScreen), true, "Fluffy Start Screen");
            window.minSize = new Vector2(650, 875);
            window.maxSize = new Vector2(650, 875);
            window.ShowUtility();
        }

        private void OnGUI() {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, 650, 880), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            bannerImage.draw(this);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(390);
            if (defaultTextStyle == null) {
                defaultTextStyle = new GUIStyle {
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.UpperCenter
                };
                boldTextStyle = new GUIStyle {
                    normal = { textColor = Color.white }, alignment = TextAnchor.UpperCenter,
                    fontSize = 26, fontStyle = FontStyle.Bold
                };
                toggleStyle = GUI.skin.toggle;
                toggleStyle.fontSize = 10;
                defaultTextStyle.fontSize = 14;
            }

            EditorGUILayout.LabelField(
                "Welcome to Fluffy - Grooming Tool. Watch the getting started videos\nby clicking the play button, or import some of the examples. ",
                defaultTextStyle);
            EditorGUILayout.EndVertical();
            drawRippleButtons();
            defaultTextStyle.fontSize = 10;
            EditorGUILayout.Space(230);
            EditorGUILayout.LabelField("Import Additional Shaders:", defaultTextStyle);
            EditorGUILayout.Space(80);
            addShowAtStartupToggle();
            addFooter();
        }

        private void drawRippleButtons() {
            importSamples.draw();
            discordButton.draw();
            documentation.draw();
            urp.enabled = hasUrpShaderPackBeenImported == 0;
            hdrp.enabled = hasHdrpShaderPackBeenImported == 0;
            builtIn.enabled = hasBuiltInShaderPackBeenImported == 0;
            hdrp.draw();
            urp.draw();
            builtIn.draw();
        }

        private void addFooter() {
            EditorGUILayout.Space(50);
            EditorGUILayout.LabelField("Made With Love By", defaultTextStyle);
            EditorGUILayout.Space(-4);
            EditorGUILayout.LabelField("DANIEL ZELLER", boldTextStyle);
        }

        private void addShowAtStartupToggle() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(1);
            bool isToggled = GUILayout.Toggle(showAtStartup, "Show At Startup", toggleStyle);
            EditorGUILayout.EndHorizontal();
            if (isToggled != showAtStartup) {
                if (isToggled) {
                    PerProjectPreferences.setInt(FurStartScreenLauncher.SHOWS_START_SCREEN, 1);
                } else {
                    PerProjectPreferences.deleteKey(FurStartScreenLauncher.SHOWS_START_SCREEN);
                }

                showAtStartup = isToggled;
            }
        }

        void Update() {
            deltaTime.Update();
            importSamples.update(deltaTime.deltaTime);
            discordButton.update(deltaTime.deltaTime);
            documentation.update(deltaTime.deltaTime);
            urp.update(deltaTime.deltaTime);
            builtIn.update(deltaTime.deltaTime);
            hdrp.update(deltaTime.deltaTime);
            if (delayedAction?.isComplete() == true) {
                delayedAction = null;
            }
        }

        private void OnDisable() {
            var isFirst = PerProjectPreferences.getInt(IS_FIRST_EVER_LAUNCH, 0) == 0;
            if (isFirst) {
                var window = FluffyWindow.launchFurPainter();
                window.showWelcomeTextUI();
                PerProjectPreferences.setInt(IS_FIRST_EVER_LAUNCH, 1);
            }
        }
    }

    [InitializeOnLoad]
    public class FurStartScreenLauncher {
        public static readonly string SHOWS_START_SCREEN = "ShowStartScreen";
        private static readonly string SHOWS_START_SCREEN_FIRST_TIME = "ShowsStartScreenFirstTime";
        public static readonly string SESSION_ID = "SESSION_ID";

        static FurStartScreenLauncher() {
            EditorApplication.update += update;
            FluffyStartScreen.completePendingPackageMove(null);
        }

        static void update() {
            EditorApplication.update -= update;

            if ((PerProjectPreferences.hasKey(SHOWS_START_SCREEN) && Time.realtimeSinceStartup < 10 && !EditorApplication.isPlayingOrWillChangePlaymode) ||
                !PerProjectPreferences.hasKey(SHOWS_START_SCREEN_FIRST_TIME)) {
                PerProjectPreferences.setInt(SHOWS_START_SCREEN_FIRST_TIME, 1);
                PerProjectPreferences.setInt(SHOWS_START_SCREEN, 1);
                FluffyStartScreen.init();
            }

            if (Time.realtimeSinceStartup < 10 && !EditorApplication.isPlayingOrWillChangePlaymode) {
                PlayerPrefs.SetInt(SESSION_ID, Random.Range(-1000, 1000));
                PlayerPrefs.Save();
            }
        }
    }
}