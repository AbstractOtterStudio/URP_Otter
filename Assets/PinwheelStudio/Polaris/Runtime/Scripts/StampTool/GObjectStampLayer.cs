#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin.StampTool
{
    [System.Serializable]
    public class GObjectStampLayer : GConditionalStampLayer, GConditionalStampLayer.IHasSplatWeightRule
    {
        [SerializeField]
        private Color visualizeColor;
        public Color VisualizeColor
        {
            get
            {
                return visualizeColor;
            }
            set
            {
                visualizeColor = value;
            }
        }

        [SerializeField]
        public List<GameObject> prototypes;
        public List<GameObject> Prototypes
        {
            get
            {
                if (prototypes == null)
                {
                    prototypes = new List<GameObject>();
                }
                return prototypes;
            }
            set
            {
                prototypes = value;
            }
        }

        [SerializeField]
        private List<int> prototypeIndices;
        public List<int> PrototypeIndices
        {
            get
            {
                if (prototypeIndices == null)
                {
                    prototypeIndices = new List<int>();
                }
                return prototypeIndices;
            }
            set
            {
                prototypeIndices = value;
            }
        }

        [SerializeField]
        private int instanceCount;
        public int InstanceCount
        {
            get
            {
                return instanceCount;
            }
            set
            {
                instanceCount = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private float minRotation;
        public float MinRotation
        {
            get
            {
                return minRotation;
            }
            set
            {
                minRotation = value;
            }
        }

        [SerializeField]
        private float maxRotation;
        public float MaxRotation
        {
            get
            {
                return maxRotation;
            }
            set
            {
                maxRotation = value;
            }
        }

        [SerializeField]
        private Vector3 minScale;
        public Vector3 MinScale
        {
            get
            {
                return minScale;
            }
            set
            {
                minScale = value;
            }
        }

        [SerializeField]
        private Vector3 maxScale;
        public Vector3 MaxScale
        {
            get
            {
                return maxScale;
            }
            set
            {
                maxScale = value;
            }
        }

        [SerializeField]
        private bool alignToSurface;
        public bool AlignToSurface
        {
            get
            {
                return alignToSurface;
            }
            set
            {
                alignToSurface = value;
            }
        }

        [SerializeField]
        private LayerMask worldRaycastMask;
        public LayerMask WorldRaycastMask
        {
            get
            {
                return worldRaycastMask;
            }
            set
            {
                worldRaycastMask = value;
            }
        }

        [SerializeField]
        private bool blendSplatWeight;
        public bool BlendSplatWeight
        {
            get
            {
                return blendSplatWeight;
            }
            set
            {
                blendSplatWeight = value;
            }
        }

        [SerializeField]
        private int splatWeightIndex;
        public int SplatWeightIndex
        {
            get
            {
                return splatWeightIndex;
            }
            set
            {
                splatWeightIndex = value;
            }
        }

        [SerializeField]
        private AnimationCurve splatWeightRemap;
        public AnimationCurve SplatWeightRemap
        {
            get
            {
                return splatWeightRemap;
            }
            set
            {
                splatWeightRemap = value;
            }
        }

        internal Texture2D splatWeightRemapTexture;

        public GObjectStampLayer() : base()
        {
            BlendSplatWeight = false;
            SplatWeightIndex = 0;
            SplatWeightRemap = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        public static GObjectStampLayer Create()
        {
            GObjectStampLayer layer = new GObjectStampLayer();
            layer.Prototypes = null;
            layer.PrototypeIndices = null;
            layer.InstanceCount = 100;
            layer.AlignToSurface = false;

#if UNITY_EDITOR
            layer.VisualizeColor = GEditorSettings.Instance.stampTools.visualizeColor;
            layer.MinRotation = GEditorSettings.Instance.stampTools.minRotation;
            layer.MaxRotation = GEditorSettings.Instance.stampTools.maxRotation;
            layer.MinScale = GEditorSettings.Instance.stampTools.minScale;
            layer.MaxScale = GEditorSettings.Instance.stampTools.maxScale;
#endif
            layer.WorldRaycastMask = 1;
            layer.UpdateCurveTextures();
            return layer;
        }

        public override void UpdateCurveTextures()
        {
            CleanUp();
            base.UpdateCurveTextures();
            splatWeightRemapTexture = GCommon.CreateTextureFromCurve(SplatWeightRemap, 256, 1);
        }

        public override void CleanUp()
        {
            base.CleanUp();

            if (splatWeightRemapTexture != null)
                GUtilities.DestroyObject(splatWeightRemapTexture);
        }

        public Texture2D GetSplatWeightRemapTexture()
        {
            return splatWeightRemapTexture;
        }
    }
}
#endif
