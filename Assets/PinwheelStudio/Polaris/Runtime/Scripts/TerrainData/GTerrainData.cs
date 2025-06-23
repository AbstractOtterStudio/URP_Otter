#if GRIFFIN
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Griffin
{
    /// <summary>
    /// The main asset type containing all settings that describe a low poly terrain.
    /// Instantiate this using Polaris.CreateAndInitTerrainData()
    /// </summary>
    [CreateAssetMenu(fileName = "Terrain Data", menuName = "Polaris/Terrain Data")]
    public class GTerrainData : ScriptableObject
    {
        public delegate void GlobalDirtyHandler(GTerrainData data, DirtyFlags flag);
        /// <summary>
        /// An event raised when some terrain was marked as dirty (modified)
        /// </summary>
        public static event GlobalDirtyHandler GlobalDirty;

        public delegate void DirtyHandler(DirtyFlags flag);
        /// <summary>
        /// An event raised when this terrain was marked dirty (modified)
        /// </summary>
        public event DirtyHandler Dirty;

        internal delegate void GrassPatchChangedHandler(int index);
        internal event GrassPatchChangedHandler GrassPatchChanged;

        internal delegate void GrassPatchGridSizeChangedHandler();
        internal event GrassPatchGridSizeChangedHandler GrassPatchGridSizeChanged;

        internal delegate void TreeChangedHandler();
        internal event TreeChangedHandler TreeChanged;

        internal delegate void PrototypeGroupChangedHandler();
        internal event PrototypeGroupChangedHandler TreePrototypeGroupChanged;
        internal event PrototypeGroupChangedHandler GrassPrototypeGroupChanged;

        /// <summary>
        /// A flag indicating what has been modified in this terrain data. 
        /// There can be more than 1 bit enabled in a single enum object, you should use Enum.HasFlag() to check for all possible case.
        /// </summary>
        [System.Flags]
        public enum DirtyFlags : byte
        {
            None = 0,
            Geometry = 1,
            GeometryTimeSliced = 2,
            Rendering = 4,
            Shading = 8,
            Foliage = 16,
            Mask = 32,
            All = byte.MaxValue
        }

        [SerializeField]
        private string id;
        /// <summary>
        /// An id assigned to this object.
        /// </summary>
        public string Id
        {
            get
            {
                return id;
            }
            internal set
            {
                id = value;
            }
        }

        [SerializeField]
        internal GGeometry geometry;
        /// <summary>
        /// Reference to the sub container contains terrain dimension, mesh settings, height map, etc.
        /// </summary>
        public GGeometry Geometry
        {
            get
            {
                if (geometry == null)
                {
                    geometry = ScriptableObject.CreateInstance<GGeometry>();
                    geometry.TerrainData = this;
                    //geometry.ResetFull();
                }
                GCommon.TryAddObjectToAsset(geometry, this);
                geometry.TerrainData = this;
                return geometry;
            }
        }

        [SerializeField]
        private GShading shading;
        /// <summary>
        /// Reference to the sub container contains terrain textures, material and material properties settings.
        /// </summary>
        public GShading Shading
        {
            get
            {
                if (shading == null)
                {
                    shading = ScriptableObject.CreateInstance<GShading>();
                    shading.TerrainData = this;
                    //shading.ResetFull();
                }
                GCommon.TryAddObjectToAsset(shading, this);
                shading.TerrainData = this;
                return shading;
            }
        }

        [SerializeField]
        private GRendering rendering;
        /// <summary>
        /// Reference to the sub container contains rendering settings such as shadow, render distance, foliage LODs, etc.
        /// </summary>
        public GRendering Rendering
        {
            get
            {
                if (rendering == null)
                {
                    rendering = ScriptableObject.CreateInstance<GRendering>();
                    rendering.TerrainData = this;
                    //rendering.ResetFull();
                }
                GCommon.TryAddObjectToAsset(rendering, this);
                rendering.TerrainData = this;
                return rendering;
            }
        }

        [SerializeField]
        private GFoliage foliage;
        /// <summary>
        /// Reference to the sub container contains foliage info, prototypes, instances list, snap settings, etc.
        /// </summary>
        public GFoliage Foliage
        {
            get
            {
                if (foliage == null)
                {
                    foliage = ScriptableObject.CreateInstance<GFoliage>();
                    foliage.TerrainData = this;
                    foliage.ResetFull();
                }
                GCommon.TryAddObjectToAsset(foliage, this);
                foliage.TerrainData = this;
                return foliage;
            }
        }

        [SerializeField]
        private GMask mask;
        /// <summary>
        /// Reference to a sub container contains utility mask for tooling.
        /// </summary>
        public GMask Mask
        {
            get
            {
                if (mask == null)
                {
                    mask = ScriptableObject.CreateInstance<GMask>();
                    mask.TerrainData = this;
                    //mask.ResetFull();
                }
                GCommon.TryAddObjectToAsset(mask, this);
                mask.TerrainData = this;
                return mask;
            }
        }

        [SerializeField]
        [FormerlySerializedAs("generatedData")]
        private GTerrainGeneratedData geometryData;
        /// <summary>
        /// Reference to the sub container contains all terrain surface meshes.
        /// </summary>
        public GTerrainGeneratedData GeometryData
        {
            get
            {
                GGeometry geo = geometry ? geometry : Geometry; //wtf!
                if (geo.StorageMode == GGeometry.GStorageMode.SaveToAsset)
                {
                    if (geometryData == null)
                    {
                        geometryData = GCommon.GetTerrainGeneratedDataAsset(this, "GeneratedGeometry");
                    }
                    geometryData.TerrainData = this;
                    return geometryData;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                geometryData = value;
            }
        }

        [ExcludeFromDoc]
        public void Reset()
        {
            id = GCommon.GetUniqueID();
        }

        /// <summary>
        /// Mark this data as dirty (modified) for asset file saving as well as performing related actions after modification.
        /// For example, SetDirty(DirtyFlags.Geometry) -> terrain object will also regenerate surface meshes.
        /// </summary>
        /// <param name="flag">Indicate what's changed, use the bitwise OR if many things have changed.</param>
        public void SetDirty(DirtyFlags flag)
        {
#if UNITY_EDITOR
            //EditorUtility.SetDirty(this);
            if ((flag == DirtyFlags.All || flag == DirtyFlags.Geometry) && geometry != null)
            {
                EditorUtility.SetDirty(geometry);
            }
            if ((flag == DirtyFlags.All || flag == DirtyFlags.Shading) && shading != null)
            {
                EditorUtility.SetDirty(shading);
            }
            if ((flag == DirtyFlags.All || flag == DirtyFlags.Rendering) && rendering != null)
            {
                EditorUtility.SetDirty(rendering);
            }
            if ((flag == DirtyFlags.All || flag == DirtyFlags.Foliage) && foliage != null)
            {
                EditorUtility.SetDirty(foliage);
            }
#endif
            if (shading != null)
            {
                shading.UpdateMaterials();
            }

            if (Dirty != null)
            {
                Dirty(flag);
            }

            if (GlobalDirty != null)
            {
                GlobalDirty(this, flag);
            }
        }

        /// <summary>
        /// Copy nummeric settings from one terrain data to other.
        /// Note that it doesn't copy textures and foliage instances.
        /// </summary>
        /// <param name="des">Destination terrain data object.</param>
        public void CopyTo(GTerrainData des)
        {
            Geometry.CopyTo(des.Geometry);
            Shading.CopyTo(des.Shading);
            Rendering.CopyTo(des.Rendering);
            Foliage.CopyTo(des.Foliage);
            Mask.CopyTo(des.Mask);
        }

        internal void InvokeGrassChange(Vector2 gridIndex)
        {
            if (GrassPatchChanged != null)
            {
                int index = GUtilities.To1DIndex((int)gridIndex.x, (int)gridIndex.y, Foliage.PatchGridSize);
                GrassPatchChanged.Invoke(index);
            }
        }

        internal void InvokeGrassPatchGridSizeChange()
        {
            if (GrassPatchGridSizeChanged != null)
            {
                GrassPatchGridSizeChanged.Invoke();
            }
        }

        internal void InvokeTreeChanged()
        {
            if (TreeChanged != null)
            {
                TreeChanged.Invoke();
            }
        }

        internal void InvokeTreePrototypeGroupChanged()
        {
            if (TreePrototypeGroupChanged != null)
            {
                TreePrototypeGroupChanged.Invoke();
            }
        }

        internal void InvokeGrassPrototypeGroupChanged()
        {
            if (GrassPrototypeGroupChanged != null)
            {
                GrassPrototypeGroupChanged.Invoke();
            }
        }

        [ExcludeFromDoc]
        public void CleanUp()
        {
            if (geometry != null && geometry.subDivisionMap != null)
            {
                GUtilities.DestroyObject(geometry.subDivisionMap);
            }
        }
    }
}
#endif
