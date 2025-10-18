#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.ExtensionSystem;
using System.Reflection;

namespace Pinwheel.Griffin.Wizard
{
    public static class GExploreTabDrawer
    {
        private static Vector2 scrollPos;

        public static void Draw()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GMarketingSettingsAsset marketing = GMarketingSettingsAsset.Instance;
            GMarketingSettingsAsset.NewsEntry[] news = marketing.GetNews();
            if (news.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(ExploreGUI.NEWS.text, GStyles.H3);
                foreach (GMarketingSettingsAsset.NewsEntry e in news)
                {
                    ExploreGUI.DrawNewsEntry(e);
                }
            }

            GMarketingSettingsAsset.AssetEntry[] featuredAssets = marketing.GetFeaturedAssets();
            if (featuredAssets.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(ExploreGUI.OTHER_PRODUCTS_FROM_PINWHEEL.text, GStyles.H3);
                foreach (GMarketingSettingsAsset.AssetEntry e in featuredAssets)
                {
                    ExploreGUI.DrawAssetEntry(e);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private class ExploreGUI
        {
            private static readonly Color HIGHLIGHT_COLOR = new Color(1, 1, 1, 0.1F);
            private static readonly Color FADE_COLOR = new Color(0, 0, 0, 0.6F);
            private static readonly Color PROMOTION_TEXT_COLOR = new Color(1, 1, 0, 1f);

            private static readonly Texture CHECKMARK = Resources.Load<Texture>("Vista/Textures/Checkmark");

            public static void DrawNewsEntry(GMarketingSettingsAsset.NewsEntry e)
            {
                Rect entryRect = EditorGUILayout.BeginVertical();
                EditorGUIUtility.AddCursorRect(entryRect, MouseCursor.Link);
                if (entryRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(entryRect, HIGHLIGHT_COLOR);
                }
                EditorGUILayout.LabelField(e.title, GStyles.P1, GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField(e.description, GStyles.P2);

                if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition) && !string.IsNullOrEmpty(e.link))
                {
                    Application.OpenURL(e.link);
                }
                EditorGUILayout.EndVertical();
            }

            public static void DrawAssetEntry(GMarketingSettingsAsset.AssetEntry e)
            {
                GUI.enabled = !e.isInstalled;
                Rect entryRect = EditorGUILayout.BeginVertical();
                EditorGUIUtility.AddCursorRect(entryRect, MouseCursor.Link);
                if (entryRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(entryRect, HIGHLIGHT_COLOR);
                }

                EditorGUILayout.BeginHorizontal();
                Rect imageRect = EditorGUILayout.GetControlRect(GUILayout.Width(64), GUILayout.Height(64));
                imageRect = new RectOffset(4, 4, 4, 4).Remove(imageRect);
                GUI.DrawTexture(imageRect, e.texture != null ? e.texture : Texture2D.whiteTexture);
                if (e.isInstalled)
                {
                    EditorGUI.DrawRect(imageRect, FADE_COLOR);
                }

                EditorGUILayout.BeginVertical();
                string title = $"{e.name} <color=orange>{e.promotionText}</color>";
                EditorGUILayout.LabelField(title, GStyles.P1, GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField(e.description, GStyles.P2);
                if (e.isInstalled)
                {
                    EditorGUILayout.GetControlRect(GUILayout.Height(2));
                    Rect checkMarkRect = EditorGUILayout.GetControlRect(GUILayout.Height(12), GUILayout.Width(12));
                    GUI.DrawTexture(checkMarkRect, CHECKMARK);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUI.enabled = true;

                if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition) && !string.IsNullOrEmpty(e.link))
                {
                    Application.OpenURL(e.link + GAssetLink.AFF);
                }
            }

            public static readonly GUIContent NEWS = new GUIContent("News");
            public static readonly GUIContent OTHER_PRODUCTS_FROM_PINWHEEL = new GUIContent("Other products from Pinwheel Studio");
        }
    }
}
#endif
