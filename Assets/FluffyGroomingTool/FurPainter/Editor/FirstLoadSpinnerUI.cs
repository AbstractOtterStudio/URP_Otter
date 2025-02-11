using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class FirstLoadSpinnerUI {
        private Material spinnerMat;
        private Texture2D emptyTexture;
        private GUIStyle percentTextStyle;
        private GUIStyle headingTextStyle;
        private readonly Color pink = new Color32(207, 115, 229, 255);

        private int textIndex = -1;

        private static readonly float LABEL_HEIGHT = 300f;
        private static readonly float FONT_SIZE_IN_PERCENT_OF_WIDTH = 0.05f;
        public static readonly int EDITOR_TIME = Shader.PropertyToID("_EditorTime");
        private static readonly string TEXT_INDEX_KEY = "TEXT_INDEX_KEY";
        
        //Thank you Twitter friends: https://twitter.com/zellah/status/1441100985737220098?s=20
        private readonly string[] whatsHappening = new string[] {
            "Thank you for\r\nusing Fluffy!",
            "What did the game dev\r\nsay to the psychologist?\r\nMy life's a mesh!",
            "What happens when\r\nyou delete a mesh?\r\nIt’s poly-gone.",
            "A Unity developer\r\nwalks into a bar and\r\ncreates a scene.",
            "A unity developer\r\nwalks into the scene\r\nand creates a bar.",
            "What’s the wettest\r\npart of a game?\r\nThe splash screen.",
            "A Unity developer wakes\r\nup the next morning.\r\nOut of memory and\r\nmissing references.",
            "Ironing normals",
            "You have so many magic\r\nnumbers in your code,\r\nthey should call\r\nyou David Copperfield!",
            "A Unity artist walks\r\ninto a bar and gets\r\npushy with all\r\nthe models..",
            "Brewing coffee",
            "Bending tangents",
            "Which game button\r\nis used most frequently\r\nto summon insects?\r\nThe B button.",
            "Preparing something",
            "Praising the UV's", 
            "Please rate Fluffy\r\non the Asset Store :)\r\nThank you!",
            "If someone tells you\r\nprogramming physics is\r\neasy, tell them \"You\r\ndon’t understand the\r\ngravity of the situation.\"",
            "What type of footwear\r\ndoes Mario have?\r\nPlatformer shoes.",
            "How do you make a\r\nsmoothie out of 3D fruit?\r\nYou put it in Blender.", 
            "A Unity artist arrives\r\nin the bar, lighting\r\nup the whole room",
            "Your support means the\r\nworld to Fluffy! Please\r\nconsider rating it\r\non the Asset Store.",
            "Sewing vertices",
            "A Unity developer walks\r\ninto a bar, realizes\r\nthis is no place for\r\nchildren and Destroys\r\nthem all.",
            "Pickup line:\r\nHi, I'm Unity, can\r\nI crash at your\r\nplace tonight?"
        };

        //Instead of extracting constants for all the magic numbers below. Lets just agree that all the numbers are percentages of width/height.
        public void drawFirstLoadProgressbar(EditorWindow window) {
            initResources();
            percentTextStyle.fontSize = (int) (window.position.width * FONT_SIZE_IN_PERCENT_OF_WIDTH);
            GUILayout.BeginVertical();

            EditorGUILayout.Space(window.position.height / 2 - LABEL_HEIGHT / 2);
            setupIndex();

            EditorGUILayout.LabelField(whatsHappening[textIndex], percentTextStyle, GUILayout.Height(LABEL_HEIGHT));
            spinnerMat.SetFloat(EDITOR_TIME, (float) EditorApplication.timeSinceStartup * 1.3f);
            EditorGUI.DrawPreviewTexture(
                new Rect(0, window.position.height / 2 - window.position.width / 2, window.position.width, window.position.width),
                emptyTexture,
                spinnerMat
            );

            GUILayout.EndVertical();
        }

        private void setupIndex() {
            if (textIndex == -1) {
                textIndex = EditorPrefs.GetInt(TEXT_INDEX_KEY);
                EditorPrefs.SetInt(TEXT_INDEX_KEY, (textIndex + 1) % (whatsHappening.Length - 1));
            }
        }

        public void resetIndex() {
            textIndex = -1;
        }

        private void initResources() {
            if (headingTextStyle == null || ReferenceEquals(spinnerMat, null) || spinnerMat == null) {
                spinnerMat = new Material(Shader.Find("Hidden/Fur-spinner-circle"));
                emptyTexture = PainterBrushTypeUI.createColorBackground(Color.black);
                percentTextStyle = createUIStyle();
                headingTextStyle = createUIStyle();
                headingTextStyle.normal.textColor = pink;
            }
        }

        private GUIStyle createUIStyle() {
            var guiStyle = new GUIStyle(GUI.skin.label) {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            return guiStyle;
        }
    }
}