using System.Collections;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class MeshCardPropertiesUI {
        private bool isExpanded = true;
        private GUIStyle headerGUIStyle;
        private GUIStyle boldStyle;
        private GUIStyle statsStyle;
        private static readonly float PREVIEW_WIDTH = 40f;
        public static readonly float PREVIEW_HEIGHT = 60f;
        private static readonly float PREVIEW_X_START = 35f;
        private static readonly float PREVIEW_Y_START = 10.5f;
        private static readonly int MAX_SUB_DIVISIONS = 25;
        public static readonly float PANEL_SIZE = 50f;
        private IEnumerator recreateCoroutine;

        private void initStyles() {
            if (headerGUIStyle == null) {
                headerGUIStyle = new GUIStyle(EditorStyles.foldoutHeader) {fontStyle = FontStyle.Bold};
                boldStyle = new GUIStyle(EditorStyles.label) {fontStyle = FontStyle.Bold};
                statsStyle = new GUIStyle(EditorStyles.label) {alignment = TextAnchor.UpperLeft, fontSize = 10};
            }
        }

        public void drawCardMeshPropertiesUI(FurCreator furCreator, GUIStyle brushPropertiesPanelStyle, PainterLayersUI painterLayersUI) {
            initStyles();
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Mesh Card Properties");
            var proceduralFurRenderer = furCreator.FurRenderer;
            if (isExpanded && furCreator.groomContainer.activeLayerIndex < proceduralFurRenderer.furContainer.layerStrandsList.Length) {
                var cmp = proceduralFurRenderer.furContainer.layerStrandsList[furCreator.groomContainer.activeLayerIndex].cardMeshProperties;
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(brushPropertiesPanelStyle);

                EditorGUI.BeginChangeCheck();
                var divY = EditorGUILayout.IntSlider(new GUIContent("Subdivisions:",
                    "How many subdivision each strand in this Layer will have. " +
                    "Increasing this has a bad impact on performance. But features like twist depend on a high " +
                    "subdivision count in order to look good."), cmp.TempSubdivisionsY, 1, MAX_SUB_DIVISIONS);

                var color = painterLayersUI.colors[furCreator.groomContainer.activeLayerIndex % 5];
                var curve = EditorGUILayout.CurveField(new GUIContent("Strand Shape Curve", "The shape curve of each strand from root to tip."),
                    cmp.shapeCurve,
                    color,
                    new Rect(0, 0, 1, 1)
                );

                var motionCurve = EditorGUILayout.CurveField(
                    new GUIContent("Motion Curve",
                        "The move ability curve of the strand. Change the curve to make the strands more/less movable from root to tip."),
                    cmp.moveCurve,
                    color,
                    new Rect(0, 0, 1, 1)
                );

                if (EditorGUI.EndChangeCheck()) {
                    FluffyUndo.safelyUndo(furCreator.FurRenderer.furContainer);
                    cmp.TempSubdivisionsY = divY;
                    cmp.RedoWatcherCardSubdivisionsY = divY;
                    if (recreateCoroutine != null) furCreator.StopCoroutine(recreateCoroutine);
                    recreateCoroutine = recreateAllDelayed(proceduralFurRenderer, cmp, furCreator);
                    furCreator.StartCoroutine(recreateCoroutine);
                }

                drawMeshCard(brushPropertiesPanelStyle, furCreator, color, cmp);
                GUILayout.EndVertical();
            }
        }

        private IEnumerator recreateAllDelayed(FurRenderer furRenderer, CardMeshProperties cardMeshProperties, FurCreator furCreator) {
            yield return new WaitForSeconds(0.5f);
            cardMeshProperties.cardSubdivisionsY = cardMeshProperties.TempSubdivisionsY;
            furRenderer.furContainer.recreateAll.Invoke();
            furCreator.needsUpdate = true;
        }

        private void drawMeshCard(GUIStyle brushPropertiesPanelStyle, FurCreator furCreator, Color32 color, CardMeshProperties cmp) {
            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal(brushPropertiesPanelStyle, GUILayout.MaxWidth(PANEL_SIZE), GUILayout.MinHeight(PANEL_SIZE));
            EditorGUILayout.Space(PANEL_SIZE);
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Mesh Statistics:", boldStyle);
            GUILayout.Label(getMeshStats(furCreator), statsStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            var lastRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint) {
                drawGrid(cmp, lastRect, color);
            }
        }

        private static string getMeshStats(FurCreator furCreator) {
            var vertCount = furCreator.FurRenderer.getVerticesCount();
            var triangleCount = furCreator.FurRenderer.getTrianglesCount();
            var ni = new CultureInfo(CultureInfo.CurrentCulture.Name).NumberFormat;

            var activeLayer = furCreator.FurRenderer.furContainer.layerStrandsList[furCreator.groomContainer.activeLayerIndex];
            var layerVerticesCount = activeLayer.layerHairStrands.Length * activeLayer.CardMesh.vertexCount;
            var layerTrianglesCount = (activeLayer.layerHairStrands.Length * activeLayer.CardMesh.triangles.Length) / 3;
            ni.NumberDecimalDigits = 0;
            ni.NumberGroupSeparator = " ";
            ni.NumberGroupSizes = new[] {3};

            return "Layer Vertices Count: " + layerVerticesCount.ToString("N", ni) +
                   "\nLayer Triangles Count: " + layerTrianglesCount.ToString("N", ni) +
                   "\n" +
                   "\nTotal Vertices Count: " + vertCount.ToString("N", ni) +
                   "\nTotal Triangles Count: " + triangleCount.ToString("N", ni);
        }

        private static void drawGrid(CardMeshProperties cmp, Rect lastRect, Color32 color) {
            var subdivisionsY = cmp.TempSubdivisionsY;
            var shapeCurve = cmp.shapeCurve;
            drawGrid(shapeCurve, subdivisionsY, lastRect, color);
        }

        public static void drawGrid(AnimationCurve shapeCurve, int subdivisionsY, Rect lastRect, Color32 color) {
            var heightStep = PREVIEW_HEIGHT / subdivisionsY;
            var cardSubdivisionsX = 1;

            for (int x = 0; x < cardSubdivisionsX + 1; x++) {
                for (int y = 0; y < subdivisionsY; y++) {
                    var cmpTempSubdivisionsY = subdivisionsY;
                    var yPercent = (float) y / cmpTempSubdivisionsY;
                    var nextYPercent = (y + 1f) / cmpTempSubdivisionsY;

                    var evaluatedPercent = shapeCurve.Evaluate(1f - yPercent);
                    var nextEvaluatedPercent = shapeCurve.Evaluate(1f - nextYPercent);
                    var widthStep = PREVIEW_WIDTH / cardSubdivisionsX * evaluatedPercent;
                    var nextWidthStep = PREVIEW_WIDTH / cardSubdivisionsX * nextEvaluatedPercent;
                    float xBendOffset = easeQuad(1f - yPercent) * 15f;
                    float nextXBendOffset = easeQuad(1f - nextYPercent) * 15f;
                    var startX = PREVIEW_X_START + lastRect.x + (1 - evaluatedPercent * PREVIEW_WIDTH / 2f);
                    var nextStartX = PREVIEW_X_START + lastRect.x + (1 - nextEvaluatedPercent * PREVIEW_WIDTH / 2f);

                    var nextXIndex = x + 1;
                    Handles.color = color;

                    if (nextXIndex <= cardSubdivisionsX) {
                        Handles.DrawLine(
                            new Vector3(startX + x * widthStep + xBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y, 0),
                            new Vector3(nextStartX + x * widthStep + nextXBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y + heightStep, 0));
                        Handles.DrawLine(
                            new Vector3(startX + x * widthStep + xBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y, 0),
                            new Vector3(nextStartX + nextXIndex * nextWidthStep + nextXBendOffset,
                                PREVIEW_Y_START + lastRect.y + heightStep * y + heightStep, 0));
                        Handles.DrawLine(
                            new Vector3(startX + x * widthStep + xBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y, 0),
                            new Vector3(startX + nextXIndex * widthStep + xBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y, 0));
                    }
                    else {
                        Handles.DrawLine(
                            new Vector3(startX + x * widthStep + xBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y, 0),
                            new Vector3(nextStartX + x * nextWidthStep + nextXBendOffset, PREVIEW_Y_START + lastRect.y + heightStep * y + heightStep,
                                0));
                    }
                }
            }

            var percent = shapeCurve.Evaluate(0f);
            var sx = PREVIEW_X_START + lastRect.x - PREVIEW_WIDTH / 2f * percent;
            var lastLineYCoord = PREVIEW_Y_START + lastRect.y + heightStep * subdivisionsY;
            Handles.DrawLine(
                new Vector3(sx, lastLineYCoord, 0),
                new Vector3(sx + PREVIEW_WIDTH * percent, lastLineYCoord, 0));
        }

        static float easeQuad(float t) {
            t /= 1f;
            return 1f * t * t;
        }
    }
}