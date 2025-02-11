using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class PainterMaskUI {
        private FluffyToolbar fluffyToolbar = new FluffyToolbar() {
            activatedColor = new Color32(153, 220, 81, 255),
            activatedColorHover = new Color32(153, 220, 81, 200)
        };

        public void drawMaskUI(FluffyWindow window, GUIStyle buttonStyle, FurCreator furCreator) {
            if (furCreator.getPainterProperties().type == (int) PaintType.MASK) {
                GUILayout.BeginVertical(window.brushTypeUI.BrushDetailsStyle);
                var tab = furCreator.getPainterProperties().maskErase ? 0 : 1;
                tab = fluffyToolbar.drawToolbar(tab, new[] {"Hide Strands", "Show Strands"}, furCreator);

                furCreator.getPainterProperties().maskErase = tab == 0;
                GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN - 2);
              
                GUILayout.BeginHorizontal();
                if (furCreator.groomContainer.getActiveLayer().isInApplyTextureMode && furCreator.IsFurStrandsProgressVisible) {
                    EditorGUILayout.LabelField("Calculating..."  );
                }
                else if (furCreator.groomContainer.getActiveLayer().isInApplyTextureMode) {
                    EditorGUILayout.LabelField("Mask Texture:", GUILayout.Width(100));
                    EditorGUI.BeginChangeCheck();
                    furCreator.groomContainer.getActiveLayer().maskTexture = (Texture2D) EditorGUILayout.ObjectField(
                        furCreator.groomContainer.getActiveLayer().maskTexture,
                        typeof(Texture2D),
                        false
                    );
                    if (GUILayout.Button(new GUIContent("Apply", "Apply the texture mask."), buttonStyle, GUILayout.Width(80))) {
                        applyTexture(furCreator);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        applyTexture(furCreator);
                    }
                }
                else {
                    if (GUILayout.Button(
                        new GUIContent("Apply Mask From Texture", "Apply a mask from a black and white texture. White means show, black means hide."),
                        buttonStyle)) {
                        furCreator.groomContainer.getActiveLayer().isInApplyTextureMode = true;
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                if (GUILayout.Button(
                    new GUIContent("Permanently Delete Masked Strands",
                        "The hidden strands will permanently be removed. This is good for performance. Changing the Hair Strand Spacing value of the Layer will add new strands again."),
                    buttonStyle)) {
                    furCreator.permanentlyDeleteMaskedStrands();
                }

                GUILayout.EndVertical();
            }
        }

        private static void applyTexture(FurCreator furCreator) {
            makeTheTextureReadable(furCreator);
            furCreator.addStrands();
        }

        private static void makeTheTextureReadable(FurCreator furCreator) {
            if (furCreator.groomContainer.getActiveLayer().maskTexture != null) {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(furCreator.groomContainer.getActiveLayer().maskTexture, out string guid,
                    out long localId);
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && !path.Contains("Library/unity")) {
                    if (AssetImporter.GetAtPath(path) is TextureImporter modelImporter) {
                        modelImporter.isReadable = true;
                    }

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive);
                }
            }
        }
    }
}