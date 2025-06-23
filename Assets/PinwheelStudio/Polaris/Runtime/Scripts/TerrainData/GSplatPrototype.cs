#if GRIFFIN
using UnityEngine;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// An object contains settings for terrain texture layer.
    /// </summary>
    [System.Serializable]
    public class GSplatPrototype
    {
        [SerializeField]
        private Texture2D texture;
        /// <summary>
        /// The color texture.
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
        private Texture2D normalMap;
        /// <summary>
        /// The normal map texture.
        /// </summary>
        public Texture2D NormalMap
        {
            get
            {
                return normalMap;
            }
            set
            {
                normalMap = value;
            }
        }

        [SerializeField]
        private Vector2 tileSize = Vector2.one;
        /// <summary>
        /// Size of the texture in world space.
        /// </summary>
        public Vector2 TileSize
        {
            get
            {
                return tileSize;
            }
            set
            {
                tileSize = value;
            }
        }

        [SerializeField]
        private Vector2 tileOffset = Vector2.zero;
        /// <summary>
        /// Offset this texture in world space.
        /// </summary>
        public Vector2 TileOffset
        {
            get
            {
                return tileOffset;
            }
            set
            {
                tileOffset = value;
            }
        }

        [SerializeField]
        private float metallic;
        /// <summary>
        /// Metallic value of the layer.
        /// </summary>
        public float Metallic
        {
            get
            {
                return metallic;
            }
            set
            {
                metallic = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float smoothness;
        /// <summary>
        /// Smoothness value of the layer.
        /// </summary>
        public float Smoothness
        {
            get
            {
                return smoothness;
            }
            set
            {
                smoothness = Mathf.Clamp01(value);
            }
        }

#if !UNITY_2018_1 && !UNITY_2018_2
        [ExcludeFromDoc]
        public bool Equals(TerrainLayer layer)
        {
            return
                texture == layer.diffuseTexture &&
                normalMap == layer.normalMapTexture &&
                tileSize == layer.tileSize &&
                tileOffset == layer.tileOffset &&
                metallic == layer.metallic &&
                smoothness == layer.smoothness;
        }

        [ExcludeFromDoc]
        public void CopyTo(TerrainLayer layer)
        {
            layer.diffuseTexture = Texture;
            layer.normalMapTexture = NormalMap;
            layer.tileSize = TileSize;
            layer.tileOffset = TileOffset;
            layer.metallic = Metallic;
            layer.smoothness = Smoothness;
        }

        /// <summary>
        /// Cast from Unity's Terrain Layer
        /// </summary>
        /// <param name="layer"></param>
        public static explicit operator GSplatPrototype(TerrainLayer layer)
        {
            GSplatPrototype proto = new GSplatPrototype();
            proto.Texture = layer.diffuseTexture;
            proto.NormalMap = layer.normalMapTexture;
            proto.TileSize = layer.tileSize;
            proto.TileOffset = layer.tileOffset;
            proto.Metallic = layer.metallic;
            proto.Smoothness = layer.smoothness;
            return proto;
        }

        /// <summary>
        /// Cast to Unity's terrain layer.
        /// </summary>
        /// <param name="proto"></param>
        public static explicit operator TerrainLayer(GSplatPrototype proto)
        {
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = proto.Texture;
            layer.normalMapTexture = proto.NormalMap;
            layer.tileSize = proto.TileSize;
            layer.tileOffset = proto.TileOffset;
            layer.metallic = proto.Metallic;
            layer.smoothness = proto.Smoothness;
            return layer;
        }
#endif

#if POLARIS
        public static explicit operator GSplatPrototype(Pinwheel.Polaris.LPTSplatInfo layer)
        {
            GSplatPrototype proto = new GSplatPrototype();
            proto.Texture = layer.Texture;
            proto.NormalMap = layer.NormalMap;
            proto.TileSize = Vector2.one;
            proto.TileOffset = Vector2.zero;
            return proto;
        }
#endif
    }
}
#endif
