#if !VISTA
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pinwheel.Griffin
{
    [ScriptedImporter(0, new string[] { "raw", "r8", "r16", "r32" })]
    public class PolarisRawImporter : ScriptedImporter
    {
        public enum BitDepth
        {
            Bit8 = 1, Bit16 = 2, Bit32 = 4
        }

        [SerializeField]
        private int m_width = 512;
        public int width
        {
            get
            {
                return m_width;
            }
            set
            {
                m_width = Mathf.Clamp(value, 1, 8192);
            }
        }

        [SerializeField]
        private int m_height = 512;
        public int height
        {
            get
            {
                return m_height;
            }
            set
            {
                m_height = Mathf.Clamp(value, 1, 8192);
            }
        }

        [SerializeField]
        private BitDepth m_bitDepth = BitDepth.Bit16;
        public BitDepth bitDepth
        {
            get
            {
                return m_bitDepth;
            }
            set
            {
                m_bitDepth = value;
            }
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            List<Object> objects = new List<Object>();
            context.GetObjects(objects);
            foreach (Object o in objects)
            {
                Object.DestroyImmediate(o);
            }
            byte[] srcData = File.ReadAllBytes(context.assetPath);
            int dstDataLength = (int)m_bitDepth * m_width * m_height;
            byte[] dstData = new byte[dstDataLength];
            int minCount = Mathf.Min(srcData.Length, dstData.Length);
            Array.Copy(srcData, dstData, minCount);

            TextureFormat format =
                m_bitDepth == BitDepth.Bit8 ? TextureFormat.R8 :
                m_bitDepth == BitDepth.Bit16 ? TextureFormat.R16 :
                m_bitDepth == BitDepth.Bit32 ? TextureFormat.RFloat : TextureFormat.RFloat;
            Texture2D tex = new Texture2D(m_width, m_height, format, false);
            tex.LoadRawTextureData(dstData);
            tex.Apply();
            context.AddObjectToAsset("Texture", tex, tex);
            context.SetMainObject(tex);
        }
    }
}
#endif
