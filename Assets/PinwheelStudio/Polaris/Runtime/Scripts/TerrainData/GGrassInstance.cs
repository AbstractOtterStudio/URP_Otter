#if GRIFFIN
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// Contains information about a single grass instance.
    /// </summary>
    [System.Serializable]
    public struct GGrassInstance
    {
        [SerializeField]
        public int prototypeIndex;
        /// <summary>
        /// Index of its prototype in the terrain prototypes list.
        /// </summary>
        public int PrototypeIndex
        {
            get
            {
                return prototypeIndex;
            }
            set
            {
                prototypeIndex = value;
            }
        }

        [SerializeField]
        public Vector3 position;
        /// <summary>
        /// Position in normalized [0-1] space.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        [SerializeField]
        public Quaternion rotation;
        /// <summary>
        /// Rotation in world space.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
            }
        }

        [SerializeField]
        public Vector3 scale;
        /// <summary>
        /// Scale in world space.
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
            }
        }

        /// <summary>
        /// Create a new grass instances.
        /// </summary>
        /// <param name="prototypeIndex">Index of target prototype in the terrain prototypes list.</param>
        /// <returns>The grass instance.</returns>
        public static GGrassInstance Create(int prototypeIndex)
        {
            GGrassInstance instance = new GGrassInstance();
            instance.PrototypeIndex = prototypeIndex;
            instance.Position = Vector3.zero;
            instance.Rotation = Quaternion.identity;
            instance.Scale = Vector3.one;
            return instance;
        }
    }
}
#endif
