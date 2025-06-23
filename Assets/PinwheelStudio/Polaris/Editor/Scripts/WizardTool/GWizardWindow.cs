#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.GroupTool;
using Pinwheel.Griffin.PaintTool;
using Pinwheel.Griffin.SplineTool;
using Pinwheel.Griffin.StampTool;

namespace Pinwheel.Griffin.Wizard
{
    public class GWizardWindow : EditorWindow
    {
        private const int TAB_CREATE = 0;
        private const int TAB_SET_SHADER = 1;
        private const int TAB_EXTENSION = 2;
        private const int TAB_EXPLORE = 3;

        private Vector2 scrollPos;
        private int selectedTab;
        private readonly string[] tabLabels = new string[]
        {
            "Create Level",
            "Set Shader",
            "Extension",
            "<color=#00ff00ff>•</color> Explore"
        };

        private static GUIStyle pageTitleStyle;
        private static GUIStyle PageTitleStyle
        {
            get
            {
                if (pageTitleStyle == null)
                {
                    pageTitleStyle = new GUIStyle(EditorStyles.label);
                    pageTitleStyle.fontStyle = FontStyle.Bold;
                    pageTitleStyle.fontSize = 18;
                    pageTitleStyle.richText = true;
                }
                return pageTitleStyle;
            }
        }

        private static GUIStyle titleStripStyle;
        private static GUIStyle TitleStripStyle
        {
            get
            {
                if (titleStripStyle == null)
                {
                    titleStripStyle = new GUIStyle();
                    titleStripStyle.margin = new RectOffset(0, 0, 8, 8);
                }
                return titleStripStyle;
            }
        }

        private Stack<IWizardPage> pages = new Stack<IWizardPage>();

        private void OnEnable()
        {
            GExtensionTabDrawer.ReloadExtension();
            GWizardEditorCommon.CheckForUrpFirstTimeImport();

            EditorApplication.update += Repaint;

            PushPage(new MainPage());
            //PushPage(new NewTerrainPage());
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private static GWizardWindow CreateWindow()
        {
            GWizardWindow window = GetWindow<GWizardWindow>();
            Texture2D icon = EditorGUIUtility.isProSkin ?
                GEditorSkin.Instance.GetTexture("IconWhite") :
                GEditorSkin.Instance.GetTexture("IconBlack");
            window.titleContent = new GUIContent(" " + GVersionInfo.ProductNameAndVersionShort, icon);
            window.minSize = new Vector2(670, 500);
            window.maxSize = new Vector2(670, 1000);
            window.wantsMouseMove = true;

            return window;
        }


        public void OnGUI()
        {
            DrawRibbon();

            IWizardPage currentPage = GetCurrentPage();

            EditorGUILayout.BeginVertical(GEditorCommon.WindowBodyStyle);
            GEditorCommon.Space();
            Rect titleStripRect = EditorGUILayout.BeginHorizontal(TitleStripStyle);
            if (pages.Count > 1)
            {
                Rect backButtonRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
                Texture2D backButtonIcon = GEditorSkin.Instance.GetTexture("BackButtonIcon");
                GUI.contentColor = EditorStyles.label.normal.textColor;
                if (GUI.Button(backButtonRect, backButtonIcon, GEditorCommon.IconButton))
                {
                    PopPage();
                }
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.LabelField(currentPage.Title, GStyles.H1, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            currentPage.OnTitleRightGUILayout(this);
            currentPage.OnTitleGUI(this, titleStripRect);
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            currentPage.OnGUI(this);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawRibbon()
        {
            string PREF_KEY = "wizard-ribbon-closed-" + System.DateTime.Today.ToString();

            GMarketingSettingsAsset marketing = GMarketingSettingsAsset.Instance;
            List<GMarketingSettingsAsset.NewsEntry> news = marketing.GetSpecialNews();
            if (news.Count > 0 && !EditorPrefs.HasKey(PREF_KEY))
            {
                Rect ribbonRect = EditorGUILayout.BeginHorizontal(GStyles.Ribbon);
                EditorGUI.DrawRect(ribbonRect, GEditorCommon.pinwheelPrimaryColor);
                EditorGUILayout.BeginVertical();
                foreach (var n in news)
                {
                    Rect r = EditorGUILayout.GetControlRect();
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
                    if (GUI.Button(r, new GUIContent(n.title, n.link), GEditorCommon.CenteredLabel))
                    {
                        Application.OpenURL(n.link);
                    }
                }

                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", EditorStyles.miniLabel, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    EditorPrefs.SetBool(PREF_KEY, true);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private IWizardPage GetCurrentPage()
        {
            if (pages == null)
            {
                pages = new Stack<IWizardPage>();
            }
            if (pages.Count == 0)
            {
                PushPage(new MainPage());
            }
            return pages.Peek();
        }

        public void PushPage(IWizardPage page)
        {
            page.OnPush(this);
            pages.Push(page);
        }

        public void PopPage()
        {
            IWizardPage currentPage = GetCurrentPage();
            if (currentPage.OnPop(this))
            {
                pages.Pop();
            }
        }

        public static void ShowMainPage(MenuCommand menuCmd)
        {
            GWizardWindow window = CreateWindow();
            window.Show();
        }

        public static void ShowSetShaderPage(GStylizedTerrain terrain)
        {
            GEditorSettings.Instance.wizardTools.setShaderTerrain = terrain;

            GWizardWindow window = CreateWindow();
            SetMaterialPage page = new SetMaterialPage();
            page.bulkMode = false;
            window.PushPage(page);
            window.Show();
        }

        public static void ShowSetShaderPage(int groupId)
        {
            GEditorSettings.Instance.wizardTools.setShaderGroupId = groupId;

            GWizardWindow window = CreateWindow();
            SetMaterialPage page = new SetMaterialPage();
            page.bulkMode = true;
            window.PushPage(page);
            window.Show();
        }

        public static void ShowExtensionPage()
        {
            GWizardWindow window = CreateWindow();
            window.PushPage(new ExtensionPage());
            window.Show();
        }
    }
}
#endif
