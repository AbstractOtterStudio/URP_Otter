#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Pinwheel.Griffin
{
    public class GImageZoomPopup : PopupWindowContent
    {
        Texture image;

        public GImageZoomPopup(Texture img)
        {
            image = img;
        }

        public override Vector2 GetWindowSize()
        {
            if (image == null)
                return Vector2.one;
            else
                return new Vector2(image.width, image.height);
        }

        public override void OnGUI(Rect rect)
        {
            if (image == null)
            {
                return;
            }

            GUI.DrawTexture(rect, image);
        }
    }
}
#endif
