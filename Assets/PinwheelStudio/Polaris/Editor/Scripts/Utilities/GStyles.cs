#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Pinwheel.Griffin
{
    public static class GStyles
    {
        private static GUIStyle h1;
        public static GUIStyle H1
        {
            get
            {
                if (h1 == null)
                {
                    h1 = new GUIStyle(EditorStyles.label);
                    h1.fontStyle = FontStyle.Bold;
                    h1.fontSize = 18;
                    h1.richText = true;
                    h1.margin = new RectOffset(0, 0, 8, 8);
                }
                return h1;
            }
        }

        private static GUIStyle h2;
        public static GUIStyle H2
        {
            get
            {
                if (h2 == null)
                {
                    h2 = new GUIStyle(EditorStyles.label);
                    h2.fontStyle = FontStyle.Bold;
                    h2.fontSize = 15;
                    h2.richText = true;
                    h1.margin = new RectOffset(0, 0, 4, 4);
                }
                return h2;
            }
        }

        private static GUIStyle h3;
        public static GUIStyle H3
        {
            get
            {
                if (h3 == null)
                {
                    h3 = new GUIStyle(EditorStyles.label);
                    h3.fontStyle = FontStyle.Bold;
                    h3.fontSize = 12;
                    h3.richText = true;
                }
                return h3;
            }
        }

        private static GUIStyle p1;
        public static GUIStyle P1
        {
            get
            {
                if (p1 == null)
                {
                    p1 = new GUIStyle(EditorStyles.label);
                    p1.wordWrap = true;
                    p1.richText = true;
                }
                return p1;
            }
        }

        private static GUIStyle p2;
        public static GUIStyle P2
        {
            get
            {
                if (p2 == null)
                {
                    p2 = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    p2.alignment = TextAnchor.UpperLeft;
                    p2.wordWrap = true;
                    p2.richText = true;
                }
                return p2;
            }
        }

        private static GUIStyle ribbon;
        public static GUIStyle Ribbon
        {
            get
            {
                if (ribbon==null)
                {
                    ribbon = new GUIStyle(EditorStyles.toolbar);
                    ribbon.fixedHeight = 0;                    
                }
                return ribbon;
            }
        }
    }
}
#endif
