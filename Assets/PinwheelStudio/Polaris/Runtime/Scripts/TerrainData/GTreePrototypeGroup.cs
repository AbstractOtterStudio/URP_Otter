#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// An asset type contain a collection of tree templates/prototypes for rendering.
    /// </summary>
    [CreateAssetMenu(fileName = "Tree Prototype Group", menuName = "Polaris/Tree Prototype Group")]
    public class GTreePrototypeGroup : ScriptableObject
    {
        public delegate void ChangedHandler(GTreePrototypeGroup sender);
        /// <summary>
        /// Subscribe to this event to get notified when any of the tree prototype has changed.
        /// </summary>
        public static event ChangedHandler Changed;
        
        [SerializeField]
        private List<GTreePrototype> prototypes;
        /// <summary>
        /// The collection of tree prototypes.
        /// </summary>
        public List<GTreePrototype> Prototypes
        {
            get
            {
                if (prototypes == null)
                    prototypes = new List<GTreePrototype>();
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
        /// Check if this is a sample asset provided within Polaris package.
        /// </summary>
        public bool IsSampleAsset => isSampleAsset;

        [ExcludeFromDoc]
        public bool Equals(TreePrototype[] treePrototypes)
        {
            if (Prototypes.Count != treePrototypes.Length)
                return false;
            for (int i = 0; i < Prototypes.Count; ++i)
            {
                if (!Prototypes[i].Equals(treePrototypes[i]))
                    return false;
            }
            return true;
        }

        [ExcludeFromDoc]
        public static GTreePrototypeGroup Create(TreePrototype[] treePrototypes)
        {
            GTreePrototypeGroup group = CreateInstance<GTreePrototypeGroup>();
            for (int i = 0; i < treePrototypes.Length; ++i)
            {
                group.Prototypes.Add((GTreePrototype)treePrototypes[i]);
            }

            return group;
        }

        /// <summary>
        /// Notify all subscribers that some tree prototypes has changed.
        /// This will also refresh the tree renderer.
        /// </summary>
        public void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
#endif
