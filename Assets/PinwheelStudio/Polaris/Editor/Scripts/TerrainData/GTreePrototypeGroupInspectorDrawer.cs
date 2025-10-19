#if GRIFFIN
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using Pinwheel.Griffin.BillboardTool;

namespace Pinwheel.Griffin
{
    public class GTreePrototypeGroupInspectorDrawer
    {
        private GTreePrototypeGroup instance;

        public GTreePrototypeGroupInspectorDrawer(GTreePrototypeGroup group)
        {
            instance = group;
        }

        public static GTreePrototypeGroupInspectorDrawer Create(GTreePrototypeGroup group)
        {
            return new GTreePrototypeGroupInspectorDrawer(group);
        }

        public void DrawGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawPrototypesListGUI();
            DrawAddPrototypeGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(instance);
                instance.NotifyChanged();
            }

            GEditorCommon.DrawAffLinks(
                "These vivid vegetations can breath life into your project",
                "https://assetstore.unity.com/packages/3d/vegetation/trees/polygon-nature-low-poly-3d-art-by-synty-120152",
                "https://assetstore.unity.com/lists/stylized-vegetation-120082");

            GEditorCommon.Separator();
            DrawConvertAssetGUI();
            GEditorCommon.DrawCommonLinks();
        }

        private void DrawPrototypesListGUI()
        {
            string label, id;
            for (int i = 0; i < instance.Prototypes.Count; ++i)
            {
                GTreePrototype p = instance.Prototypes[i];
                CachePrefabPath(p);

                label = p.Prefab != null && !string.IsNullOrEmpty(p.Prefab.name) ? p.Prefab.name : "Tree " + i;
                id = "treeprototype" + i + instance.GetInstanceID().ToString();

                int index = i;
                GenericMenu menu = new GenericMenu();
                menu.AddItem(
                    new GUIContent("Remove"),
                    false,
                    () => { ConfirmAndRemovePrototypeAtIndex(index); });
                menu.AddItem(
                    new GUIContent("Sync with Prefab"),
                    false,
                    () => { p.Refresh(); });

                GEditorCommon.Foldout(label, false, id, () =>
                {
                    if (p.Prefab != null)
                    {
                        DrawPreview(p.Prefab);
                    }                    

                    GEditorCommon.Header("Prefab");
                    p.Prefab = EditorGUILayout.ObjectField("Prefab", p.Prefab, typeof(GameObject), false) as GameObject;
                    GUI.enabled = false;
                    EditorGUI.indentLevel += 1;
                    if (p.HasLod0)
                    {
                        EditorGUILayout.LabelField("LOD0");
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.ObjectField("Mesh", p.SharedMesh, typeof(Mesh), false);
                        for (int i = 0; i < p.SharedMaterials.Length; ++i)
                        {
                            string materialLabel = i == 0 ? (p.SharedMaterials.Length > 1 ? "Materials" : "Material") : " ";
                            EditorGUILayout.ObjectField(materialLabel, p.SharedMaterials[i], typeof(Material), false);
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                    if (p.HasLod1)
                    {
                        EditorGUILayout.LabelField("LOD1");
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.ObjectField("Mesh", p.SharedMeshLod1, typeof(Mesh), false);
                        for (int i = 0; i < p.SharedMaterialsLod1.Length; ++i)
                        {
                            string materialLabel = i == 0 ? (p.SharedMaterialsLod1.Length > 1 ? "Materials" : "Material") : " ";
                            EditorGUILayout.ObjectField(materialLabel, p.SharedMaterialsLod1[i], typeof(Material), false);
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                    EditorGUI.indentLevel -= 1;
                    GUI.enabled = true;
                    p.ShadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadow", p.ShadowCastingMode);
                    p.ReceiveShadow = EditorGUILayout.Toggle("Receive Shadow", p.ReceiveShadow);

                    GEditorCommon.Header("Billboard");
                    EditorGUILayout.BeginHorizontal();
                    p.Billboard = EditorGUILayout.ObjectField("Billboard Asset", p.Billboard, typeof(BillboardAsset), false) as BillboardAsset;
                    if (GUILayout.Button("Create", GUILayout.Width(75), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    {
                        GBillboardEditor bbEditor = GBillboardEditor.GetWindow<GBillboardEditor>();
                        bbEditor.target = p.Prefab;
                        bbEditor.Show();
                        bbEditor.RenderPreview();
                    }
                    EditorGUILayout.EndHorizontal();
                    p.BillboardShadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadow", p.BillboardShadowCastingMode);
                    p.BillboardReceiveShadow = EditorGUILayout.Toggle("Receive Shadow", p.BillboardReceiveShadow);

                    GEditorCommon.Header("Base Transform");
                    p.PivotOffset = EditorGUILayout.FloatField("Pivot Offset", p.PivotOffset);
                    p.BaseRotation = Quaternion.Euler(GEditorCommon.InlineVector3Field("Base Rotation", p.BaseRotation.eulerAngles));
                    p.BaseScale = GEditorCommon.InlineVector3Field("Base Scale", p.BaseScale);

                    GEditorCommon.Header("Utilities");
                    GUI.enabled = !p.KeepPrefabLayer;
                    p.Layer = EditorGUILayout.LayerField("Layer", p.Layer);
                    GUI.enabled = true;
                    p.KeepPrefabLayer = EditorGUILayout.Toggle("Keep Prefab Layer", p.KeepPrefabLayer);

                    GUI.enabled = false;
                    EditorGUILayout.Toggle("Has Collider", p.HasCollider);
                    GUI.enabled = true;
                }, menu);
            }
        }

        private void CachePrefabPath(GTreePrototype p)
        {
            if (p.Prefab == null)
            {
                p.Editor_PrefabAssetPath = null;
            }
            else
            {
                p.Editor_PrefabAssetPath = AssetDatabase.GetAssetPath(p.Prefab);
            }
        }

        private void ConfirmAndRemovePrototypeAtIndex(int index)
        {
            GTreePrototype p = instance.Prototypes[index];
            string label = p.Prefab != null ? p.Prefab.name : "Tree " + index;
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove " + label,
                "OK", "Cancel"))
            {
                instance.Prototypes.RemoveAt(index);
            }
        }

        private void DrawPreview(GameObject g)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.selectionGridTileSizeMedium.y));
            GEditorCommon.DrawPreview(r, g);
        }

        private void DrawAddPrototypeGUI()
        {
            EditorGUILayout.GetControlRect(GUILayout.Height(1));
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
            GameObject g = GEditorCommon.ObjectSelectorDragDrop<GameObject>(r, "Drop a Game Object here!", "t:GameObject");
            if (g != null)
            {
                GTreePrototype p = GTreePrototype.Create(g);
                instance.Prototypes.Add(p);
            }
        }

        private void DrawConvertAssetGUI()
        {
            if (GUILayout.Button("Create Prefab Prototype Group"))
            {
                ConvertToPrefabPrototypeGroup();
            }
        }

        private void ConvertToPrefabPrototypeGroup()
        {
            GPrefabPrototypeGroup group = ScriptableObject.CreateInstance<GPrefabPrototypeGroup>();
            for (int i = 0; i < instance.Prototypes.Count; ++i)
            {
                GameObject prefab = instance.Prototypes[i].Prefab;
                if (prefab != null)
                {
                    group.Prototypes.Add(prefab);
                }
            }

            string path = AssetDatabase.GetAssetPath(instance);
            string directory = Path.GetDirectoryName(path);
            string filePath = Path.Combine(directory, string.Format("{0}_{1}_{2}.asset", instance.name, "Prefabs", GCommon.GetUniqueID()));
            AssetDatabase.CreateAsset(group, filePath);

            Selection.activeObject = group;
        }
    }
}
#endif
