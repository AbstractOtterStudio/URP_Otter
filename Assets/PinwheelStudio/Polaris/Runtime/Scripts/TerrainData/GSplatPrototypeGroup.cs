#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// An asset type containing a collection of splat prototype.
    /// </summary>
    [CreateAssetMenu(fileName = "Splat Prototype Group", menuName = "Polaris/Splat Prototype Group")]
    public class GSplatPrototypeGroup : ScriptableObject
    {
        [SerializeField]
        private List<GSplatPrototype> prototypes;
        /// <summary>
        /// The collection of splat prototype.
        /// </summary>
        public List<GSplatPrototype> Prototypes
        {
            get
            {
                if (prototypes == null)
                {
                    prototypes = new List<GSplatPrototype>();
                }
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
        /// Check if this asset is a sample asset provided within Polaris package.
        /// </summary>
        public bool IsSampleAsset => isSampleAsset;

#if !UNITY_2018_1 && !UNITY_2018_2
        [ExcludeFromDoc]
        public bool Equals(TerrainLayer[] layers)
        {
            if (Prototypes.Count != layers.Length)
                return false;
            for (int i = 0; i < layers.Length; ++i)
            {
                if (!Prototypes[i].Equals(layers[i]))
                    return false;
            }
            return true;
        }

        [ExcludeFromDoc]
        public static GSplatPrototypeGroup Create(TerrainLayer[] layers)
        {
            GSplatPrototypeGroup group = CreateInstance<GSplatPrototypeGroup>();
            for (int i = 0; i < layers.Length; ++i)
            {
                group.Prototypes.Add((GSplatPrototype)layers[i]);
            }

            return group;
        }
#endif
    }
}
#endif
