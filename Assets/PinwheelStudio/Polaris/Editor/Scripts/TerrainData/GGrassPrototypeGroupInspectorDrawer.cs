#if GRIFFIN
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace Pinwheel.Griffin
{
    public class GGrassPrototypeGroupInspectorDrawer
    {
        private GGrassPrototypeGroup instance;

        public GGrassPrototypeGroupInspectorDrawer(GGrassPrototypeGroup group)
        {
            instance = group;
        }

        public static GGrassPrototypeGroupInspectorDrawer Create(GGrassPrototypeGroup group)
        {
            return new GGrassPrototypeGroupInspectorDrawer(group);
        }

        public void DrawGUI()
        {
            DrawInstruction();
            EditorGUI.BeginChangeCheck();
            DrawPrototypesListGUI();
            DrawAddPrototypeGUI(); 
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(instance);
                instance.NotifyChanged();
            }

            GEditorCommon.DrawAffLinks(
                "These lively vegetation assets can bring your project to life",
                "https://assetstore.unity.com/packages/3d/vegetation/trees/polygon-nature-low-poly-3d-art-by-synty-120152",
                "https://assetstore.unity.com/lists/stylized-vegetation-120082");

            GEditorCommon.Separator();
            DrawConvertAssetGUI();
            GEditorCommon.DrawCommonLinks();
        }

        private void DrawInstruction()
        {
            string label = "Instruction";
            string id = "instruction" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                EditorGUILayout.LabelField("Some properties require Foliage Data to be processed on a terrain to take effect.\nGo to Terrain > Foliage > CONTEXT > Update Grasses to do it.", GEditorCommon.WordWrapItalicLabel);
                EditorGUILayout.LabelField("GPU Instancing is required for grass rendering.", GEditorCommon.WarningLabel);
            });
        }

        private void DrawPrototypesListGUI()
        {
            for (int i = 0; i < instance.Prototypes.Count; ++i)
            {
                GGrassPrototype p = instance.Prototypes[i];

                string label = string.Empty;
                if (p.Shape != GGrassShape.DetailObject)
                    label = p.Texture != null && !string.IsNullOrEmpty(p.Texture.name) ? p.Texture.name : "Grass " + i;
                else
                    label = p.Prefab != null && !string.IsNullOrEmpty(p.Prefab.name) ? p.Prefab.name : "Grass " + i;
                string id = "grassprototype" + i + instance.GetInstanceID().ToString();

                int index = i;
                GenericMenu menu = new GenericMenu();
                menu.AddItem(
                    new GUIContent("Remove"),
                    false,
                    () => { ConfirmAndRemovePrototypeAtIndex(index); });

                GEditorCommon.Foldout(label, false, id, () =>
                {
                    if (p.Shape != GGrassShape.DetailObject)
                    {
                        p.Texture = EditorGUILayout.ObjectField("Texture", p.Texture, typeof(Texture2D), false) as Texture2D;
                        p.Shape = (GGrassShape)EditorGUILayout.EnumPopup("Shape", p.Shape); 
                        if (p.Shape == GGrassShape.DetailObject)
                            return;

                        if (p.Shape == GGrassShape.CustomMesh)
                        {
                            p.CustomMesh = EditorGUILayout.ObjectField("Mesh LOD0", p.CustomMesh, typeof(Mesh), false) as Mesh;
                            p.CustomMeshLod1 = EditorGUILayout.ObjectField("Mesh LOD1", p.CustomMeshLod1, typeof(Mesh), false) as Mesh;
                        }
                    }
                    else
                    {
                        DrawPreview(p.Prefab);
                        p.Shape = (GGrassShape)EditorGUILayout.EnumPopup("Shape", p.Shape);
                        if (p.Shape != GGrassShape.DetailObject)
                            return;

                        p.Prefab = EditorGUILayout.ObjectField("Prefab", p.Prefab, typeof(GameObject), false) as GameObject;

                        EditorGUI.indentLevel += 1;
                        GUI.enabled = false;
                        if (p.HasLod0)
                        {
                            EditorGUILayout.LabelField("LOD0");
                            EditorGUI.indentLevel += 1;
                            EditorGUILayout.ObjectField("Mesh", p.DetailMesh, typeof(Mesh), false);
                            for (int i = 0; i < p.DetailMaterials.Length; ++i)
                            {
                                string materialLabel = i == 0 ? (p.DetailMaterials.Length > 1 ? "Materials" : "Material") : " ";
                                EditorGUILayout.ObjectField(materialLabel, p.DetailMaterials[i], typeof(Material), false);
                            }

                            if (p.DetailMaterials != null)
                            {
                                bool hasInstancingTurnedOff = false;
                                foreach (Material m in p.DetailMaterials)
                                {
                                    if (!m.enableInstancing)
                                        hasInstancingTurnedOff = true;
                                }
                                if (hasInstancingTurnedOff)
                                {
                                    EditorGUILayout.LabelField(" ","Material(s) has GPU Instancing option turned off. This will cause rendering error!.", GEditorCommon.WarningLabel);
                                }
                            }
                            EditorGUI.indentLevel -= 1;
                        }
                        if (p.HasLod1)
                        {
                            EditorGUILayout.LabelField("LOD1");
                            EditorGUI.indentLevel += 1;
                            EditorGUILayout.ObjectField("Mesh", p.DetailMeshLod1, typeof(Mesh), false);
                            for (int i = 0; i < p.DetailMaterialsLod1.Length; ++i)
                            {
                                string materialLabel = i == 0 ? (p.DetailMaterialsLod1.Length > 1 ? "Materials" : "Material") : " ";
                                EditorGUILayout.ObjectField(materialLabel, p.DetailMaterialsLod1[i], typeof(Material), false);
                            }
                            if (p.DetailMaterialsLod1 != null)
                            {
                                bool hasInstancingTurnedOff = false;
                                foreach (Material m in p.DetailMaterialsLod1)
                                {
                                    if (!m.enableInstancing)
                                        hasInstancingTurnedOff = true;
                                }
                                if (hasInstancingTurnedOff)
                                {
                                    EditorGUILayout.LabelField(" ", "Material(s) has GPU Instancing option turned off. This will cause rendering error!.", GEditorCommon.WarningLabel);
                                }
                            }
                            EditorGUI.indentLevel -= 1;
                        }

                        GUI.enabled = true;

                        EditorGUILayout.LabelField(" ", "Detail Object uses the first Mesh Filter/Renderer for LOD0, second ones for LOD1, and may NOT affected by wind.", GEditorCommon.WordWrapItalicLabel);                        
                        EditorGUI.indentLevel -= 1;
                    }
                    p.Color = EditorGUILayout.ColorField("Color", p.Color);

                    p.ShadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadow", p.ShadowCastingMode);
                    p.ReceiveShadow = EditorGUILayout.Toggle("Receive Shadow", p.ReceiveShadow);

                    if (p.Shape != GGrassShape.DetailObject)
                    {
                        p.IsBillboard = EditorGUILayout.Toggle("Billboard", p.IsBillboard);
                    }

                    //---
                    if (p.Shape == GGrassShape.DetailObject)
                    {

                    }

                    if (p.Shape != GGrassShape.DetailObject && p.IsBillboard)
                    {
                        EditorGUILayout.LabelField("Billboard will NOT work if Interactive Grass is enabled on the terrain.", GEditorCommon.WarningLabel);
                    }

                    //---
                    GEditorCommon.Header("Base Transform");
                    p.Size = GEditorCommon.InlineVector3Field("Size", p.Size);
                    p.PivotOffset = EditorGUILayout.DelayedFloatField("Pivot Offset", p.PivotOffset);

                    GEditorCommon.Header("Utilities");
                    p.BendFactor = EditorGUILayout.FloatField("Bend Factor", p.BendFactor);
                    p.Layer = EditorGUILayout.LayerField("Layer", p.Layer);
                    p.AlignToSurface = EditorGUILayout.Toggle("Align To Surface", p.AlignToSurface);
                },
                menu);
            }
        }

        private void ConfirmAndRemovePrototypeAtIndex(int index)
        {
            GGrassPrototype p = instance.Prototypes[index];
            string label = p.Texture != null ? p.Texture.name : "Grass " + index;
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove " + label,
                "OK", "Cancel"))
            {
                instance.Prototypes.RemoveAt(index);
                RefreshInstanceList();
                EditorUtility.SetDirty(instance);
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
            Rect r0 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
            Texture2D t = GEditorCommon.ObjectSelectorDragDrop<Texture2D>(r0, "Drop a Texture here!", "t:Texture2D");
            if (t != null)
            {
                GGrassPrototype g = GGrassPrototype.Create(t);
                instance.Prototypes.Add(g);
                RefreshInstanceList();
                EditorUtility.SetDirty(instance);
            }

            EditorGUILayout.GetControlRect(GUILayout.Height(1));
            Rect r1 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
            GameObject prefab = GEditorCommon.ObjectSelectorDragDrop<GameObject>(r1, "Drop a Game Object here!", "t:GameObject");
            if (prefab != null)
            {
                GGrassPrototype p = GGrassPrototype.Create(prefab);
                instance.Prototypes.Add(p);
                RefreshInstanceList();
                EditorUtility.SetDirty(instance);
            }
        }

        private void RefreshInstanceList()
        {
            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (t.TerrainData != null && t.TerrainData.Foliage.Grasses == instance)
                {
                    t.TerrainData.Foliage.Refresh();
                }
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
                if (instance.Prototypes[i].Shape != GGrassShape.DetailObject)
                    continue;
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
