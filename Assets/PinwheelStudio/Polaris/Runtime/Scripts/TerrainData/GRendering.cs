#if GRIFFIN
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// Sub asset type containing configurations of terrain and foliage rendering, such as shadow options, render distance, LOD distance, etc.
    /// This should not be used alone without a parent terrain data object. In most case you dont instantiate this with ScriptableObject.Create.
    /// The correct way to access this is terrain.TerrainData.Rendering
    /// </summary>
    public class GRendering : ScriptableObject
    {
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
        private bool castShadow;
        /// <summary>
        /// Should terrain geometry cast shadow?
        /// </summary>
        public bool CastShadow
        {
            get
            {
                return castShadow;
            }
            set
            {
                castShadow = value;
            }
        }

        [SerializeField]
        private bool receiveShadow;
        /// <summary>
        /// Should terrain geometry receive shadow?
        /// </summary>
        public bool ReceiveShadow
        {
            get
            {
                return receiveShadow;
            }
            set
            {
                receiveShadow = value;
            }
        }

        [FormerlySerializedAs("drawFoliage")]
        [SerializeField]
        private bool drawTrees;
        /// <summary>
        /// Should it draw trees using builtin renderer?
        /// </summary>
        public bool DrawTrees
        {
            get
            {
                return drawTrees;
            }
            set
            {
                drawTrees = value;
            }
        }

        [SerializeField]
        private bool drawGrasses = true;
        /// <summary>
        /// Should it draw grass using builtin renderer?
        /// </summary>
        public bool DrawGrasses
        {
            get
            {
                return drawGrasses;
            }
            set
            {
                drawGrasses = value;
            }
        }

        [SerializeField]
        private bool enableInstancing;
        [System.Obsolete]
        [ExcludeFromDoc]
        public bool EnableInstancing
        {
            get
            {
                if (!SystemInfo.supportsInstancing)
                    enableInstancing = false;
                return enableInstancing;
            }
            set
            {
                if (SystemInfo.supportsInstancing)
                {
                    enableInstancing = value;
                }
                else
                {
                    enableInstancing = false;
                }
            }
        }

        [SerializeField]
        private float treeLod1Start;
        /// <summary>
        /// The distance from camera where it starts rendering tree at LOD1
        /// </summary>
        public float TreeLod1Start
        {
            get
            {
                return treeLod1Start;
            }
            set
            {
                treeLod1Start = Mathf.Clamp(value, 0, billboardStart);
            }
        }

        [SerializeField]
        private float billboardStart;
        /// <summary>
        /// The distance from camera where it starts rendering tree as billboard.
        /// </summary>
        public float BillboardStart
        {
            get
            {
                return billboardStart;
            }
            set
            {
                billboardStart = Mathf.Clamp(value, 0, treeDistance);
            }
        }

        [SerializeField]
        private float treeDistance;
        /// <summary>
        /// Maximum distance from camera where tree is visible.
        /// </summary>
        public float TreeDistance
        {
            get
            {
                return treeDistance;
            }
            set
            {
                treeDistance = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private float grassDistance;
        /// <summary>
        /// Maximum distance from camera where grass is visible.
        /// </summary>
        public float GrassDistance
        {
            get
            {
                return grassDistance;
            }
            set
            {
                grassDistance = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private float grassLod1Start;
        /// <summary>
        /// Distance from camera where it starts rendering grass at LOD1.
        /// </summary>
        public float GrassLod1Start
        {
            get
            {
                return grassLod1Start;
            }
            set
            {
                grassLod1Start = Mathf.Clamp(value, 0, grassDistance);
            }
        }

        [SerializeField]
        private float grassFadeStart;
        /// <summary>
        /// Distance from the camera where grass start fading out, in relative range of [0, grassDistance]
        /// </summary>
        public float GrassFadeStart
        {
            get
            {
                return grassFadeStart;
            }
            set
            {
                grassFadeStart = Mathf.Clamp01(value);
            }
        }

        [ExcludeFromDoc]
        public void Reset()
        {
            name = "Rendering";
            CastShadow = GRuntimeSettings.Instance.renderingDefault.terrainCastShadow;
            ReceiveShadow = GRuntimeSettings.Instance.renderingDefault.terrainReceiveShadow;
            DrawTrees = GRuntimeSettings.Instance.renderingDefault.drawTrees;
            TreeDistance = GRuntimeSettings.Instance.renderingDefault.treeDistance;
            BillboardStart = GRuntimeSettings.Instance.renderingDefault.billboardStart;
            TreeLod1Start = GRuntimeSettings.Instance.renderingDefault.treeLod1Start;
            DrawGrasses = GRuntimeSettings.Instance.renderingDefault.drawGrasses;
            GrassDistance = GRuntimeSettings.Instance.renderingDefault.grassDistance;
            GrassLod1Start = GRuntimeSettings.Instance.renderingDefault.grassLod1Start;
            GrassFadeStart = GRuntimeSettings.Instance.renderingDefault.grassFadeStart;
        }

        [ExcludeFromDoc]
        public void ResetFull()
        {
            Reset();
        }

        /// <summary>
        /// Copy numeric settings to other object.
        /// </summary>
        /// <param name="des">The other object.</param>
        public void CopyTo(GRendering des)
        {
            des.CastShadow = CastShadow;
            des.ReceiveShadow = ReceiveShadow;
            des.DrawTrees = DrawTrees;
            des.TreeLod1Start = TreeLod1Start;
            des.BillboardStart = BillboardStart;
            des.TreeDistance = TreeDistance;
            des.GrassDistance = GrassDistance;
            des.GrassLod1Start = GrassLod1Start;
            des.GrassFadeStart = GrassFadeStart;
        }
    }
}
#endif
