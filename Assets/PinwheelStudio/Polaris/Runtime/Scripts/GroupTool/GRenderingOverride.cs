#if GRIFFIN
using UnityEngine;

namespace Pinwheel.Griffin.GroupTool
{
    [System.Serializable]
    public struct GRenderingOverride
    {
        [SerializeField]
        private bool overrideCastShadow;
        public bool OverrideCastShadow
        {
            get
            {
                return overrideCastShadow;
            }
            set
            {
                overrideCastShadow = value;
            }
        }

        [SerializeField]
        private bool castShadow;
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
        private bool overrideReceiveShadow;
        public bool OverrideReceiveShadow
        {
            get
            {
                return overrideReceiveShadow;
            }
            set
            {
                overrideReceiveShadow = value;
            }
        }

        [SerializeField]
        private bool receiveShadow;
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

        [SerializeField]
        private bool overrideDrawTrees;
        public bool OverrideDrawTrees
        {
            get
            {
                return overrideDrawTrees;
            }
            set
            {
                overrideDrawTrees = value;
            }
        }

        [SerializeField]
        private bool drawTrees;
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
        [System.Obsolete]
        private bool overrideEnableInstancing;
        [System.Obsolete]
        public bool OverrideEnableInstancing
        {
            get
            {
                return overrideEnableInstancing;
            }
            set
            {
                overrideEnableInstancing = value;
            }
        }

        [SerializeField]
        [System.Obsolete]
        private bool enableInstancing;
        [System.Obsolete]
        public bool EnableInstancing
        {
            get
            {
                return enableInstancing;
            }
            set
            {
                enableInstancing = value;
            }
        }

        [SerializeField]
        private bool overrideTreeLod1Start;
        public bool OverrideTreeLod1Start
        {
            get
            {
                return overrideTreeLod1Start;
            }
            set
            {
                overrideTreeLod1Start = value;
            }
        }

        [SerializeField]
        private float treeLod1Start;
        public float TreeLod1Start
        {
            get
            {
                return treeLod1Start;
            }
            set
            {
                treeLod1Start = Mathf.Clamp(value, 0, GCommon.MAX_TREE_DISTANCE);
            }
        }

        [SerializeField]
        private bool overrideBillboardStart;
        public bool OverrideBillboardStart
        {
            get
            {
                return overrideBillboardStart;
            }
            set
            {
                overrideBillboardStart = value;
            }
        }

        [SerializeField]
        private float billboardStart;
        public float BillboardStart
        {
            get
            {
                return billboardStart;
            }
            set
            {
                billboardStart = Mathf.Clamp(value, 0, GCommon.MAX_TREE_DISTANCE);
            }
        }

        [SerializeField]
        private bool overrideTreeDistance;
        public bool OverrideTreeDistance
        {
            get
            {
                return overrideTreeDistance;
            }
            set
            {
                overrideTreeDistance = value;
            }
        }

        [SerializeField]
        private float treeDistance;
        public float TreeDistance
        {
            get
            {
                return treeDistance;
            }
            set
            {
                treeDistance = Mathf.Clamp(value, 0, GCommon.MAX_TREE_DISTANCE);
            }
        }

        [SerializeField]
        private bool overrideDrawGrasses;
        public bool OverrideDrawGrasses
        {
            get
            {
                return overrideDrawGrasses;
            }
            set
            {
                overrideDrawGrasses = value;
            }
        }

        [SerializeField]
        private bool drawGrasses;
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
        private bool overrideGrassLod1Start;
        public bool OverrideGrassLod1Start
        {
            get
            {
                return overrideGrassLod1Start;
            }
            set
            {
                overrideGrassLod1Start = value;
            }
        }

        [SerializeField]
        private float grassLod1Start;
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
        private bool overrideGrassDistance;
        public bool OverrideGrassDistance
        {
            get
            {
                return overrideGrassDistance;
            }
            set
            {
                overrideGrassDistance = value;
            }
        }

        [SerializeField]
        private float grassDistance;
        public float GrassDistance
        {
            get
            {
                return grassDistance;
            }
            set
            {
                grassDistance = Mathf.Clamp(value, 0, GCommon.MAX_GRASS_DISTANCE);
            }
        }

        [SerializeField]
        private bool overrideGrassFadeStart;
        public bool OverrideGrassFadeStart
        {
            get
            {
                return overrideGrassFadeStart;
            }
            set
            {
                overrideGrassFadeStart = value;
            }
        }

        [SerializeField]
        private float grassFadeStart;
        public float GrassFadeStart
        {
            get
            {
                return grassFadeStart;
            }
            set
            {
                grassFadeStart = Mathf.Clamp(value, 0f, 1f);
            }
        }

        public void Reset()
        {
            OverrideCastShadow = false;
            OverrideReceiveShadow = false;
            OverrideDrawTrees = false;
            OverrideTreeLod1Start = false;
            OverrideBillboardStart = false;
            OverrideTreeDistance = false;
            OverrideDrawGrasses = false;
            OverrideGrassDistance = false;
            OverrideGrassFadeStart = false;

            CastShadow = GRuntimeSettings.Instance.renderingDefault.terrainCastShadow;
            ReceiveShadow = GRuntimeSettings.Instance.renderingDefault.terrainReceiveShadow;
            DrawTrees = GRuntimeSettings.Instance.renderingDefault.drawTrees;
            TreeLod1Start = GRuntimeSettings.Instance.renderingDefault.treeLod1Start;
            BillboardStart = GRuntimeSettings.Instance.renderingDefault.billboardStart;
            TreeDistance = GRuntimeSettings.Instance.renderingDefault.treeDistance;
            DrawGrasses = GRuntimeSettings.Instance.renderingDefault.drawGrasses;
            GrassDistance = GRuntimeSettings.Instance.renderingDefault.grassDistance;
            GrassFadeStart = GRuntimeSettings.Instance.renderingDefault.grassFadeStart;
        }

        public void Override(GRendering r)
        {
            if (OverrideCastShadow)
                r.CastShadow = CastShadow;
            if (OverrideReceiveShadow)
                r.ReceiveShadow = ReceiveShadow;
            if (OverrideDrawTrees)
                r.DrawTrees = DrawTrees;
            if (OverrideTreeLod1Start)
                r.TreeLod1Start = TreeLod1Start;
            if (OverrideBillboardStart)
                r.BillboardStart = BillboardStart;
            if (OverrideTreeDistance)
                r.TreeDistance = TreeDistance;
            
            if (OverrideDrawGrasses)
                r.DrawGrasses = DrawGrasses;
            if (OverrideGrassLod1Start)
                r.GrassLod1Start = GrassLod1Start;
            if (OverrideGrassDistance)
                r.GrassDistance = GrassDistance;
            if (OverrideGrassFadeStart)
                r.GrassFadeStart = GrassFadeStart;
        }
    }
}
#endif
