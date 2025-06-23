#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// An asset type contains a collection of prototype for grass rendering.
    /// Create a new one in the editor with context menu, or in runtime with ScriptableObject.Create
    /// Assign to a terrain at runtime with terrain.TerrainData.Foliage.Grasses
    /// </summary>
    [CreateAssetMenu(fileName = "Grass Prototype Group", menuName = "Polaris/Grass Prototype Group")]
    public class GGrassPrototypeGroup : ScriptableObject
    {
        public delegate void ChangedHandler(GGrassPrototypeGroup sender);
        /// <summary>
        /// Subscribe to this event to get notified when any prototype has changed.
        /// </summary>
        public static event ChangedHandler Changed;

        [SerializeField]
        private List<GGrassPrototype> prototypes;
        /// <summary>
        /// A collection of prototype containing info for grass rendering.
        /// </summary>
        public List<GGrassPrototype> Prototypes
        {
            get
            {
                if (prototypes == null)
                    prototypes = new List<GGrassPrototype>();
                return prototypes;
            }
            set
            {
                prototypes = value;
            }
        }

        [SerializeField]
        private bool isSampleAsset;
        /// <summary>
        /// Check if this asset is provided as a sample asset in Polaris package.
        /// </summary>
        public bool IsSampleAsset => isSampleAsset;

        public bool Equals(DetailPrototype[] detailPrototypes)
        {
            if (Prototypes.Count != detailPrototypes.Length)
                return false;
            for (int i = 0; i < Prototypes.Count; ++i)
            {
                if (!Prototypes[i].Equals(detailPrototypes[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Create a new asset from Unity's terrain Detail Prototype array.
        /// </summary>
        /// <param name="detailPrototypes">The detail prototype array retrieve from Unity's Terrain component.</param>
        /// <returns>A new prototype group asset.</returns>
        public static GGrassPrototypeGroup Create(DetailPrototype[] detailPrototypes)
        {
            GGrassPrototypeGroup group = CreateInstance<GGrassPrototypeGroup>();
            for (int i = 0; i < detailPrototypes.Length; ++i)
            {
                group.Prototypes.Add((GGrassPrototype)detailPrototypes[i]);
            }

            return group;
        }

        /// <summary>
        /// Call this function to refresh grass renderer & notify other subscribers that grass prototype has changed..
        /// </summary>
        public void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
#endif
