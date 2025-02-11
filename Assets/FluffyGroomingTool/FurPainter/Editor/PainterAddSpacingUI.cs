using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class PainterAddSpacingUI {
        public float paintDistanceBetweenStrands = 0.3f;
        public void drawAddFurSpacingUI(PainterBrushTypeUI brushMenu, PainterProperties p) {
            if (p.type == (int) PaintType.ADD_FUR) { 
                GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
                paintDistanceBetweenStrands = EditorGUILayout.Slider("Strands spacing:", paintDistanceBetweenStrands, 0, 1);
                GUILayout.EndVertical(); 
            }
        }
    }
}