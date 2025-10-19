using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public class ArtUtil : MonoBehaviour {


    public string fileName;
    public float offset;
    public float offsetY;

    [ContextMenu("CaptureScreenshot")]
    public void CaptureScreenshot()
    {

        ScreenCapture.CaptureScreenshot(fileName);
    }

    [ContextMenu("Layout")]
    public void Layout()
    {

        Object[] objs = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);
        int count = objs.Length;
        int col = 5;
        int row = Mathf.CeilToInt( count / 4.0f);

        int width = (int)(row * offset);

        for (int i = 0; i < objs.Length; i++)
        {
            int tmpRow = i / col;
            int tmpCol = i % col;
            GameObject go = objs[i] as GameObject;
            if (go != null)
            {
                GameObject tmpGo = Instantiate(go);
                tmpGo.transform.position = new Vector3(tmpRow * offset, 0, tmpCol * offsetY);
            }          
        }
    }


}
