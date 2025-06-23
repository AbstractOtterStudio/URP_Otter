#if GRIFFIN
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// Represent a tree rendered by the terrain.
    /// </summary>
    [System.Serializable]
    public struct GTreeInstance
    {
        [SerializeField]
        public int prototypeIndex;
        /// <summary>
        /// Index of the tree prototype in the terrain's prototype list.
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
        /// Position of the tree in normalized [0-1] space.
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
        /// Create a new tree instance.
        /// </summary>
        /// <param name="prototypeIndex"></param>
        /// <returns></returns>
        public static GTreeInstance Create(int prototypeIndex)
        {
            GTreeInstance tree = new GTreeInstance();
            tree.PrototypeIndex = prototypeIndex;
            tree.Position = Vector3.zero;
            tree.Rotation = Quaternion.identity;
            tree.Scale = Vector3.one;

            return tree;
        }

        /// <summary>
        /// Cast from Unity's tree instance.
        /// </summary>
        /// <param name="t"></param>
        public static explicit operator GTreeInstance(TreeInstance t)
        {
            GTreeInstance tree = Create(t.prototypeIndex);
            tree.Position = t.position;
            tree.Rotation = Quaternion.Euler(0, t.rotation * Mathf.Rad2Deg, 0);
            tree.Scale = new Vector3(t.widthScale, t.heightScale, t.widthScale);

            return tree;
        }

        /// <summary>
        /// Cast to Unity's tree instance.
        /// </summary>
        /// <param name="t"></param>
        public static explicit operator TreeInstance(GTreeInstance t)
        {
            TreeInstance tree = new TreeInstance();
            tree.prototypeIndex = t.PrototypeIndex;
            tree.position = t.Position;
            tree.widthScale = t.Scale.x;
            tree.heightScale = t.Scale.y;
            tree.color = Color.white;

            return tree;
        }

        internal static int GetStructSize()
        {
            return sizeof(int) + sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 3;
        }
    }
}
#endif
