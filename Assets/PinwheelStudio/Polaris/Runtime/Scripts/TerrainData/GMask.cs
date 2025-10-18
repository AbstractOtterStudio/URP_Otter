#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// Sub asset type containing utility mask texture for terrain tooling.
    /// This should not be used alone without a parent terrain data. In most case you don't instantiate it with ScriptableObject.Create
    /// The correct way to access this is terrain.TerrainData.Mask
    /// </summary>
    public class GMask : ScriptableObject
    {
        /// <summary>
        /// Name of the mask map.
        /// </summary>
        public const string MASK_MAP_NAME = "Mask Map";

        [SerializeField]
        private GTerrainData terrainData;
        /// <summary>
        /// The parent terrain data object.
        /// </summary>
        public GTerrainData TerrainData
        {
            get
            {
                return terrainData;
            }
            internal set
            {
                terrainData = value;
            }
        }

        [SerializeField]
        private int maskMapResolution;
        /// <summary>
        /// Size of the mask map in pixels. When this value changed, the mask map will be resized and resampled.
        /// </summary>
        public int MaskMapResolution
        {
            get
            {
                return maskMapResolution;
            }
            set
            {
                int oldValue = maskMapResolution;
                maskMapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(value), GCommon.TEXTURE_SIZE_MIN, GCommon.TEXTURE_SIZE_MAX);
                if (oldValue != maskMapResolution)
                {
                    ResampleMaskMap();
                }
            }
        }

        [SerializeField]
        private Texture2D maskMap;
        /// <summary>
        /// Direct reference to the mask map texture. The mask color are used as following:
        /// - R: lock terrain region from being edited.
        /// - G: smooth/sharp normal mask.
        /// - B: erosion tool's water source.
        /// - A: unused.
        /// </summary>
        public Texture2D MaskMap
        {
            get
            {
                if (maskMap == null)
                {
                    maskMap = GCommon.CreateTexture(MaskMapResolution, Color.clear, TextureFormat.RGBA32);
                    maskMap.filterMode = FilterMode.Bilinear;
                    maskMap.wrapMode = TextureWrapMode.Clamp;
                    maskMap.name = MASK_MAP_NAME;
                }
                GCommon.TryAddObjectToAsset(maskMap, TerrainData);
                return maskMap;
            }
        }

        /// <summary>
        /// True if the mask map was allocated.
        /// </summary>
        public bool HasMaskMap => maskMap != null;

        /// <summary>
        /// Return a texture for reading mask. It's the mask itself if allocated, otherwise a default black texture.
        /// Only read this texture with bilinear filter.
        /// </summary>
        public Texture2D MaskMapOrDefault
        {
            get
            {
                if (maskMap == null)
                {
                    return GRuntimeSettings.Instance.defaultTextures.blackTexture;
                }
                else
                {
                    return maskMap;
                }
            }
        }

        [ExcludeFromDoc]
        public void Reset()
        {
            name = "Mask";
            MaskMapResolution = GRuntimeSettings.Instance.maskDefault.maskMapResolution;
        }

        [ExcludeFromDoc]
        public void ResetFull()
        {
            Reset();
            if (maskMap!=null)
            {
                GUtilities.DestroyObject(maskMap);
            }
        }

        /// <summary>
        /// Copy numeric settings to other object.
        /// This does not copy texture.
        /// </summary>
        /// <param name="des"></param>
        public void CopyTo(GMask des)
        {
            des.MaskMapResolution = MaskMapResolution;
        }

        private void ResampleMaskMap()
        {
            if (maskMap == null)
                return;
            Texture2D tmp = new Texture2D(MaskMapResolution, MaskMapResolution, TextureFormat.RGBA32, false);
            RenderTexture rt = new RenderTexture(MaskMapResolution, MaskMapResolution, 32, RenderTextureFormat.ARGB32);
            GCommon.CopyToRT(maskMap, rt);
            GCommon.CopyFromRT(tmp, rt);
            rt.Release();
            Object.DestroyImmediate(rt);

            tmp.name = maskMap.name;
            tmp.filterMode = maskMap.filterMode;
            tmp.wrapMode = maskMap.wrapMode;
            Object.DestroyImmediate(maskMap, true);
            maskMap = tmp;
            GCommon.TryAddObjectToAsset(maskMap, TerrainData);
        }

        [ExcludeFromDoc]
        public float GetMaskMapMemStats()
        {
            if (maskMap == null)
                return 0;
            return maskMap.width * maskMap.height * 4;
        }

        [ExcludeFromDoc]
        public void RemoveMaskMap()
        {
            if (maskMap != null)
            {
                GUtilities.DestroyObject(maskMap);
            }
        }
    }
}
#endif
