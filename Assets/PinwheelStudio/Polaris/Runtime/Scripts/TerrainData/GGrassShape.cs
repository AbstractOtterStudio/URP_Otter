#if GRIFFIN
namespace Pinwheel.Griffin
{
    /// <summary>
    /// Shape of grass instance, used for selecting grass mesh on rendering.
    /// </summary>
    public enum GGrassShape
    {
        /// <summary>
        /// Use a single quad
        /// </summary>
        Quad, 
        /// <summary>
        /// Use 2 quads crossing each other
        /// </summary>
        Cross, 
        /// <summary>
        /// Use 3 quads crossing each others.
        /// </summary>
        TriCross, 
        /// <summary>
        /// Use many quads in somewhat random distribution. This value is recommended as it give thicker grass with less instance count.
        /// </summary>
        Clump, 
        /// <summary>
        /// Use the mesh & material provided in detail object prefab.
        /// </summary>
        DetailObject, 
        /// <summary>
        /// Use a custom provided mesh with builtin grass material.
        /// </summary>
        CustomMesh
    }
}
#endif
