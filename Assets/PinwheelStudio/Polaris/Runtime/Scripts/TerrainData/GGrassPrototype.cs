#if GRIFFIN
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// A template containing common data for a type of grass.
    /// </summary>
    [System.Serializable]
    public class GGrassPrototype
    {
        [SerializeField]
        private Texture2D texture;
        /// <summary>
        /// The texture to render grass.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return texture;
            }
            set
            {
                texture = value;
            }
        }

        [SerializeField]
        private GameObject prefab;
        /// <summary>
        /// The prefab to render in Detail Object mode.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public GameObject Prefab
        {
            get
            {
                return prefab;
            }
            set
            {
                prefab = value;
                RefreshDetailObjectSettings();
            }
        }

        [SerializeField]
        internal Vector3 size;
        /// <summary>
        /// Base size of grass instance.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public Vector3 Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }

        [SerializeField]
        internal int layer;
        /// <summary>
        /// The object layer used for rendering.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public int Layer
        {
            get
            {
                return layer;
            }
            set
            {
                layer = value;
            }
        }

        [SerializeField]
        private GGrassShape shape;
        /// <summary>
        /// Shape of the grass, each shape use different mesh for rendering.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public GGrassShape Shape
        {
            get
            {
                return shape;
            }
            set
            {
                shape = value;
            }
        }

        [SerializeField]
        private Mesh customMesh;
        /// <summary>
        /// The mesh to use for render in Shape.CustomMesh mode.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public Mesh CustomMesh
        {
            get
            {
                return customMesh;
            }
            set
            {
                customMesh = value;
            }
        }

        [SerializeField]
        private Mesh customMeshLod1;
        /// <summary>
        /// The mesh to use for render in Shape.CustomMesh mode, when an instance is far from camera.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public Mesh CustomMeshLod1
        {
            get
            {
                return customMeshLod1;
            }
            set
            {
                customMeshLod1 = value;
            }
        }

        [SerializeField]
        private Mesh detailMesh;
        /// <summary>
        /// The mesh used for rendering in Detail Object mode, retrieve from provided prefab.
        /// </summary>
        public Mesh DetailMesh
        {
            get
            {
                return detailMesh;
            }
            private set
            {
                detailMesh = value;
            }
        }

        [SerializeField]
        private Mesh detailMeshLod1;
        /// <summary>
        /// The mesh used for rendering in Detail Object mode when the instance is far from camera, retrieve from provided prefab.
        /// </summary>
        public Mesh DetailMeshLod1
        {
            get
            {
                return detailMeshLod1;
            }
            private set
            {
                detailMeshLod1 = value;
            }
        }

        [System.Obsolete]
        [SerializeField]
        private Material detailMaterial;
        [System.Obsolete]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Material DetailMaterial
        {
            get
            {
                return detailMaterial;
            }
            private set
            {
                detailMaterial = value;
            }
        }

        [SerializeField]
        private Material[] detailMaterials;
        /// <summary>
        /// Materials used for rendering in Detail Object mode, retrieve from provided prefab.
        /// </summary>
        public Material[] DetailMaterials
        {
            get
            {
                return detailMaterials;
            }
            private set
            {
                detailMaterials = value;
            }
        }

        [SerializeField]
        private Material[] detailMaterialsLod1;
        /// <summary>
        /// Materials used for rendering in Detail Object mode when an instance is far from camera, retrieve from provided prefab.
        /// </summary>
        public Material[] DetailMaterialsLod1
        {
            get
            {
                return detailMaterialsLod1;
            }
            private set
            {
                detailMaterialsLod1 = value;
            }
        }

        [SerializeField]
        internal ShadowCastingMode shadowCastingMode;
        /// <summary>
        /// Should this type of grass cast shadow?
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public ShadowCastingMode ShadowCastingMode
        {
            get
            {
                return shadowCastingMode;
            }
            set
            {
                shadowCastingMode = value;
            }
        }

        [SerializeField]
        internal bool receiveShadow;
        /// <summary>
        /// Should this type of grass receive shadow?
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
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

        [SerializeField]
        private bool alignToSurface;
        /// <summary>
        /// Should this type of grass aligns its normal vector to the surface orientation? 
        /// This value is used when performing grass snapping.
        /// </summary>
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
        internal float pivotOffset;
        /// <summary>
        /// Offset the instance height along Y-axis on rendering.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public float PivotOffset
        {
            get
            {
                return pivotOffset;
            }
            set
            {
                pivotOffset = Mathf.Clamp(value, -1, 1);
            }
        }

        [SerializeField]
        private float bendFactor = 1;
        /// <summary>
        /// How much it react to wind.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public float BendFactor
        {
            get
            {
                return bendFactor;
            }
            set
            {
                bendFactor = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private Color color = Color.white;
        /// <summary>
        /// Tint color for this type of grass.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        [SerializeField]
        private bool isBillboard;
        /// <summary>
        /// Should this grass type rendered as billboard? Billboard works best with Quad shape.
        /// You should call GGrassPrototypeGroup.NotifyChanged() to refresh the renderer.
        /// </summary>
        public bool IsBillboard
        {
            get
            {
                return isBillboard;
            }
            set
            {
                isBillboard = value;
            }
        }

        /// <summary>
        /// Does it have enough info (mesh/material) to render at LOD0?
        /// </summary>
        public bool HasLod0
        {
            get
            {
                if (shape != GGrassShape.DetailObject)
                {
                    return true;
                }
                else
                {
                    return DetailMesh != null && DetailMaterials != null && DetailMaterials.Length > 0;
                }
            }
        }

        /// <summary>
        /// Does it have enough info (mesh/material) to render at LOD1?
        /// </summary>
        public bool HasLod1
        {
            get
            {
                if (shape != GGrassShape.DetailObject)
                {
                    return true;
                }
                else
                {
                    return DetailMeshLod1 != null && DetailMaterialsLod1 != null && DetailMaterialsLod1.Length > 0;
                }
            }
        }

        /// <summary>
        /// Create a new prototype from a grass texture.
        /// </summary>
        /// <param name="tex">The grass texture.</param>
        /// <returns>The new prototype.</returns>
        public static GGrassPrototype Create(Texture2D tex)
        {
            GGrassPrototype prototype = new GGrassPrototype();
            prototype.Shape = GGrassShape.Quad;
            prototype.Texture = tex;
            prototype.ShadowCastingMode = ShadowCastingMode.On;
            prototype.ReceiveShadow = true;
            prototype.Size = Vector3.one;
            prototype.Layer = LayerMask.NameToLayer("Default");
            prototype.AlignToSurface = false;
            prototype.PivotOffset = 0;
            prototype.BendFactor = 1;
            prototype.Color = Color.white;
            return prototype;
        }

        /// <summary>
        /// Create a new prototype from a prefab. This prototype will be rendered as Detail Object.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static GGrassPrototype Create(GameObject prefab)
        {
            GGrassPrototype prototype = new GGrassPrototype();
            prototype.Shape = GGrassShape.DetailObject;
            prototype.Prefab = prefab;
            prototype.Size = Vector3.one;
            prototype.Layer = LayerMask.NameToLayer("Default");
            prototype.AlignToSurface = false;
            prototype.PivotOffset = 0;
            prototype.BendFactor = 1;
            prototype.Color = Color.white;
            return prototype;
        }

        /// <summary>
        /// Get the mesh that will be used for rendering at LOD0.
        /// </summary>
        /// <returns>The mesh that will be used for rendering.</returns>
        public Mesh GetBaseMesh()
        {
            if (Shape == GGrassShape.DetailObject)
            {
                return DetailMesh;
            }
            if (Shape == GGrassShape.CustomMesh)
            {
                return CustomMesh != null ? CustomMesh : GRuntimeSettings.Instance.foliageRendering.GetGrassMesh(GGrassShape.Quad);
            }
            else
            {
                return GRuntimeSettings.Instance.foliageRendering.GetGrassMesh(Shape);
            }
        }

        /// <summary>
        /// Get the mesh that will be used for rendering at LOD1.
        /// </summary>
        /// <returns>The mesh that will be used for rendering.</returns>
        public Mesh GetBaseMeshLod1()
        {
            if (Shape == GGrassShape.DetailObject)
            {
                return DetailMeshLod1;
            }
            if (Shape == GGrassShape.CustomMesh)
            {
                return CustomMeshLod1 != null ? CustomMeshLod1 :
                    CustomMesh != null ? CustomMesh :
                    GRuntimeSettings.Instance.foliageRendering.GetGrassMeshLod1(GGrassShape.Quad);
            }
            else
            {
                return GRuntimeSettings.Instance.foliageRendering.GetGrassMeshLod1(Shape);
            }
        }

        /// <summary>
        /// Refresh rendering info (mesh/material) from prefab.
        /// </summary>
        public void RefreshDetailObjectSettings()
        {
            if (Prefab == null)
            {
                DetailMesh = null;
                DetailMaterials = null;
                DetailMeshLod1 = null;
                DetailMaterialsLod1 = null;
            }
            else
            {
                MeshFilter[] meshFilters = Prefab.GetComponentsInChildren<MeshFilter>();
                MeshRenderer[] meshRenderers = Prefab.GetComponentsInChildren<MeshRenderer>();

                if (meshFilters.Length > 0)
                {
                    DetailMesh = meshFilters[0].sharedMesh;
                }

                if (meshRenderers.Length > 0)
                {
                    DetailMaterials = meshRenderers[0].sharedMaterials;
                    ShadowCastingMode = meshRenderers[0].shadowCastingMode;
                    ReceiveShadow = meshRenderers[0].receiveShadows;
                }

                if (meshFilters.Length > 1)
                {
                    DetailMeshLod1 = meshFilters[1].sharedMesh;
                }

                if (meshRenderers.Length > 1)
                {
                    DetailMaterialsLod1 = meshRenderers[1].sharedMaterials;
                }
            }
        }

        /// <summary>
        /// Cast from Unity's Detail Prototype
        /// </summary>
        /// <param name="p"></param>
        public static explicit operator GGrassPrototype(DetailPrototype p)
        {
            GGrassPrototype proto = new GGrassPrototype();
            proto.Color = p.healthyColor;
            proto.Shape = p.usePrototypeMesh ? GGrassShape.DetailObject : GGrassShape.Quad;
            proto.Texture = p.prototypeTexture;
            proto.Prefab = p.prototype;
            proto.Size = new Vector3(p.maxWidth, p.maxHeight, p.maxWidth);
            proto.Layer = LayerMask.NameToLayer("Default");
            proto.AlignToSurface = false;
            proto.BendFactor = 1;
            proto.IsBillboard = p.renderMode == DetailRenderMode.GrassBillboard;
            return proto;
        }

        /// <summary>
        /// Cast to Unity's Detail Prototype
        /// </summary>
        /// <param name="p"></param>
        public static explicit operator DetailPrototype(GGrassPrototype p)
        {
            DetailPrototype proto = new DetailPrototype();
            proto.usePrototypeMesh = p.Shape == GGrassShape.DetailObject;
            proto.prototypeTexture = p.Texture;
            proto.prototype = p.Prefab;
            proto.minWidth = p.size.x;
            proto.maxWidth = p.size.x * 2;
            proto.minHeight = p.size.y;
            proto.maxHeight = p.size.y * 2;
            proto.healthyColor = p.color;
            if (p.IsBillboard && p.Shape != GGrassShape.DetailObject)
                proto.renderMode = DetailRenderMode.GrassBillboard;

            return proto;
        }

        public bool Equals(DetailPrototype detailPrototype)
        {
            bool modeEqual =
                (Shape == GGrassShape.Quad && !detailPrototype.usePrototypeMesh) ||
                (Shape == GGrassShape.DetailObject && detailPrototype.usePrototypeMesh);
            return
                modeEqual &&
                Texture == detailPrototype.prototypeTexture &&
                Prefab == detailPrototype.prototype &&
                Size == new Vector3(detailPrototype.maxWidth, detailPrototype.maxHeight, detailPrototype.maxWidth);
        }
    }
}
#endif
