#if GRIFFIN
namespace Pinwheel.Griffin
{
    /// <summary>
    /// The behavior when snapping tree/grass to below surface.
    /// </summary>
    public enum GSnapMode
    {
        /// <summary>
        /// Snap to the containing terrain.
        /// </summary>
        Terrain, 
        /// <summary>
        /// Snap to world colliders or containing terrain.
        /// </summary>
        World
    }
}
#endif
