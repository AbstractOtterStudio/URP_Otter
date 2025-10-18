#if GRIFFIN
using UnityEngine;
using UnityEngine.Rendering;
using Pinwheel.Griffin.Physic;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// A template containing information about a kind of tree.
    /// </summary>
    [System.Serializable]
    public class GTreePrototype
    {
        [SerializeField]
        internal GameObject prefab;
        /// <summary>
        /// The tree prefab, should contain all neccessary component such as Mesh Filter, Mesh Renderer, Capsule Collider.
        /// LODGroup component is not supported, instead it will take the first and second mesh renderer for its LOD0 and LOD1
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public GameObject Prefab
        {
            get
            {
                return prefab;
            }
            set
            {
                GameObject oldValue = prefab;
                GameObject newValue = value;
                prefab = newValue;
                if (oldValue != newValue)
                {
                    Refresh();
                }
            }
        }

        [HideInInspector]
        [SerializeField]
        internal Mesh sharedMesh;
        /// <summary>
        /// The mesh for rendering at LOD0, get from provided prefab.
        /// </summary>
        public Mesh SharedMesh
        {
            get
            {
                return sharedMesh;
            }
            private set
            {
                sharedMesh = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal Material[] sharedMaterials;
        /// <summary>
        /// The materials for rendering at LOD0, get from provided prefab.
        /// </summary>
        public Material[] SharedMaterials
        {
            get
            {
                return sharedMaterials;
            }
            private set
            {
                sharedMaterials = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal Mesh sharedMeshLod1;
        /// <summary>
        /// The mesh for rendering at LOD1, get from provided prefab.
        /// </summary>
        public Mesh SharedMeshLod1
        {
            get
            {
                return sharedMeshLod1;
            }
            private set
            {
                sharedMeshLod1 = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal Material[] sharedMaterialsLod1;
        /// <summary>
        /// The materials for rendering at LOD1, get from provided prefab. 
        /// </summary>
        public Material[] SharedMaterialsLod1
        {
            get
            {
                return sharedMaterialsLod1;
            }
            private set
            {
                sharedMaterialsLod1 = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal ShadowCastingMode shadowCastingMode;
        /// <summary>
        /// Should it cast shadow?
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
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

        [HideInInspector]
        [SerializeField]
        internal bool receiveShadow;
        /// <summary>
        /// Should it receive shadow?
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
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

        [HideInInspector]
        [SerializeField]
        internal ShadowCastingMode billboardShadowCastingMode;
        /// <summary>
        /// Should it cast shadow when rendered as billboard?
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public ShadowCastingMode BillboardShadowCastingMode
        {
            get
            {
                return billboardShadowCastingMode;
            }
            set
            {
                billboardShadowCastingMode = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        internal bool billboardReceiveShadow;
        /// <summary>
        /// Should it receive shadow when rendered as billboard?
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public bool BillboardReceiveShadow
        {
            get
            {
                return billboardReceiveShadow;
            }
            set
            {
                billboardReceiveShadow = value;
            }
        }

        [SerializeField]
        internal int layer;
        /// <summary>
        /// The layer to render this tree in.
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
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
        private bool keepPrefabLayer;
        /// <summary>
        /// If on, it will use the source prefab layer.
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public bool KeepPrefabLayer
        {
            get
            {
                return keepPrefabLayer;
            }
            set
            {
                keepPrefabLayer = value;
            }
        }

        [SerializeField]
        internal BillboardAsset billboard;
        /// <summary>
        /// Billboard asset for the tree. You can create one with the Billboard Creator tool in the editor.
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public BillboardAsset Billboard
        {
            get
            {
                return billboard;
            }
            set
            {
                billboard = value;
            }
        }

        [SerializeField]
        internal bool hasCollider;
        /// <summary>
        /// True if the provided prefab has a Capsule Collider attached.
        /// </summary>
        public bool HasCollider
        {
            get
            {
                return hasCollider;
            }
            private set
            {
                hasCollider = value;
            }
        }

        [SerializeField]
        internal GTreeColliderInfo colliderInfo;
        /// <summary>
        /// Describe the capsule collider attached to this tree.
        /// </summary>
        public GTreeColliderInfo ColliderInfo
        {
            get
            {
                return colliderInfo;
            }
            private set
            {
                colliderInfo = value;
            }
        }

        [SerializeField]
        private float pivotOffset;
        /// <summary>
        /// Offset the tree position along Y axis on rendering.
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public float PivotOffset
        {
            get
            {
                return pivotOffset;
            }
            set
            {
                pivotOffset = value;
            }
        }

        [SerializeField]
        private Quaternion baseRotation = Quaternion.identity;
        /// <summary>
        /// Initial rotation of each tree. This will be combined with per-instance rotation on rendering. 
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public Quaternion BaseRotation
        {
            get
            {
                if (baseRotation.x == 0 &&
                    baseRotation.y == 0 &&
                    baseRotation.z == 0 &&
                    baseRotation.w == 0)
                {
                    baseRotation = Quaternion.identity;
                }
                return baseRotation;
            }
            set
            {
                baseRotation = value;
                if (baseRotation.x == 0 &&
                     baseRotation.y == 0 &&
                     baseRotation.z == 0 &&
                     baseRotation.w == 0)
                {
                    baseRotation = Quaternion.identity;
                }
            }
        }

        [SerializeField]
        private Vector3 baseScale = Vector3.one;
        /// <summary>
        /// Initial scale of each tree. This will be combined with per-instance scale on rendering.
        /// You have to call the GTreePrototypeGroup.NofifyChanged() to refresh the renderer.
        /// </summary>
        public Vector3 BaseScale
        {
            get
            {
                return baseScale;
            }
            set
            {
                baseScale = value;
            }
        }

#if UNITY_EDITOR
        [SerializeField]
        private string editor_PrefabAssetPath;
        [ExcludeFromDoc]
        public string Editor_PrefabAssetPath
        {
            get
            {
                return editor_PrefabAssetPath;
            }
            set
            {
                editor_PrefabAssetPath = value;
            }
        }
#endif

        /// <summary>
        /// Bounding box of the tree mesh.
        /// </summary>
        public Bounds BaseBounds
        {
            get
            {
                if (Prefab != null)
                {
                    MeshFilter mf = Prefab.GetComponentInChildren<MeshFilter>();
                    if (mf != null)
                    {
                        return mf.sharedMesh.bounds;
                    }
                }
                return new Bounds();
            }
        }

        /// <summary>
        /// Does it have enough info for rendering?
        /// </summary>
        public bool IsValid
        {
            get
            {
                return Prefab != null && SharedMesh != null && SharedMaterials != null;
            }
        }

        /// <summary>
        /// Check if it has enough info to render at LOD0
        /// </summary>
        public bool HasLod0
        {
            get
            {
                return sharedMesh != null && sharedMaterials != null && sharedMaterials.Length != 0;
            }
        }

        /// <summary>
        /// Check if it has enough info to render at LOD1
        /// </summary>
        public bool HasLod1
        {
            get
            {
                return sharedMeshLod1 != null && sharedMaterialsLod1 != null && sharedMaterialsLod1.Length != 0;
            }
        }

        /// <summary>
        /// Create a new prototype from a game object or prefab.
        /// </summary>
        /// <param name="g">The source game object or prefab.</param>
        /// <returns>The new prototype</returns>
        public static GTreePrototype Create(GameObject g)
        {
            GTreePrototype prototype = new GTreePrototype();
            prototype.Prefab = g;
            prototype.PivotOffset = 0;
            prototype.BaseRotation = Quaternion.identity;
            prototype.BaseScale = Vector3.one;
            return prototype;
        }

        /// <summary>
        /// Read the prefab and refesh all settings.
        /// </summary>
        public void Refresh()
        {
            if (Prefab == null)
            {
                SharedMesh = null;
                SharedMaterials = null;
                SharedMeshLod1 = null;
                SharedMaterialsLod1 = null;
                ShadowCastingMode = ShadowCastingMode.Off;
                ReceiveShadow = false;
                Layer = 0;
            }
            else
            {
                MeshFilter[] meshFilters = Prefab.GetComponentsInChildren<MeshFilter>();
                MeshRenderer[] meshRenderers = Prefab.GetComponentsInChildren<MeshRenderer>();

                if (meshFilters.Length > 0)
                {
                    SharedMesh = meshFilters[0].sharedMesh;
                }

                if (meshRenderers.Length > 0)
                {
                    SharedMaterials = meshRenderers[0].sharedMaterials;
                    ShadowCastingMode = meshRenderers[0].shadowCastingMode;
                    ReceiveShadow = meshRenderers[0].receiveShadows;
                }

                if (meshFilters.Length > 1)
                {
                    SharedMeshLod1 = meshFilters[1].sharedMesh;
                }

                if (meshRenderers.Length > 1)
                {
                    SharedMaterialsLod1 = meshRenderers[1].sharedMaterials;
                }

                CapsuleCollider col = Prefab.GetComponentInChildren<CapsuleCollider>();
                hasCollider = col != null;
                if (col != null)
                {
                    ColliderInfo = new GTreeColliderInfo(col);
                }

                if (KeepPrefabLayer)
                {
                    Layer = Prefab.layer;
                }
            }

            if (BaseScale == Vector3.zero)
                BaseScale = Vector3.one;
        }

        /// <summary>
        /// Cast from Unity's tree prototype.
        /// </summary>
        /// <param name="p"></param>
        public static explicit operator GTreePrototype(TreePrototype p)
        {
            return Create(p.prefab);
        }

        /// <summary>
        /// Cast to Unity's tree prototype.
        /// </summary>
        /// <param name="p"></param>
        public static explicit operator TreePrototype(GTreePrototype p)
        {
            TreePrototype prototype = new TreePrototype();
            prototype.prefab = p.Prefab;
            return prototype;
        }

        [ExcludeFromDoc]
        public bool Equals(TreePrototype treePrototype)
        {
            return Prefab == treePrototype.prefab;
        }

        [ExcludeFromDoc]
        public BoundingSphere GetBoundingSphere()
        {
            if (SharedMesh == null)
            {
                return new BoundingSphere(Vector3.zero, 0);
            }
            else
            {
                Bounds b = SharedMesh.bounds;
                Vector3 pos = b.center;
                float radius = Vector3.Distance(b.max, b.center);
                return new BoundingSphere(pos, radius);
            }
        }
    }
}
#endif
