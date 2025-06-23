using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin.Rendering
{
    struct GCullFlags
    {
        public const byte CULLED = 0;
        public const byte VISIBLE = 1;
        public const byte PARTIALLY_VISIBLE = 2;
    }

    public static class GCullUtils
    {
        /// <summary>
        /// Test if an object, represent by an AABB, is visible inside a camera frustum or not
        /// </summary>
        /// <param name="plane0"></param>
        /// <param name="plane1"></param>
        /// <param name="plane2"></param>
        /// <param name="plane3"></param>
        /// <param name="plane4"></param>
        /// <param name="plane5"></param>
        /// <param name="b"></param>
        /// <returns>Visibility state as listed in the CullFlags struct</returns>
        public static byte TestFrustumAABB(ref Plane plane0, ref Plane plane1, ref Plane plane2, ref Plane plane3, ref Plane plane4, ref Plane plane5, ref Bounds b)
        {
            //the 8 corners of AABB
            Vector3 p0 = b.min;
            Vector3 p1 = b.min + Vector3.right * b.size.x;
            Vector3 p2 = b.min + Vector3.forward * b.size.z;
            Vector3 p3 = b.min + Vector3.right * b.size.x + Vector3.forward * b.size.z;
            Vector3 p4 = p0 + Vector3.up * b.size.y;
            Vector3 p5 = p1 + Vector3.up * b.size.y;
            Vector3 p6 = p2 + Vector3.up * b.size.y;
            Vector3 p7 = p3 + Vector3.up * b.size.y;

            //If all points are on the back side of a plane, it's not visible
            if (IsBehindPlaneAABB(ref plane0, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;
            if (IsBehindPlaneAABB(ref plane1, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;
            if (IsBehindPlaneAABB(ref plane2, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;
            if (IsBehindPlaneAABB(ref plane3, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;
            if (IsBehindPlaneAABB(ref plane4, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;
            if (IsBehindPlaneAABB(ref plane5, ref p0, ref p1, ref p2, ref p3, ref p4, ref p5, ref p6, ref p7)) return GCullFlags.CULLED;

            //At least 1 point falls outside the frustum, then it's partially visible
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p0)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p1)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p2)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p3)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p4)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p5)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p6)) return GCullFlags.PARTIALLY_VISIBLE;
            if (!IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref p7)) return GCullFlags.PARTIALLY_VISIBLE;

            //Otherwise fully visible
            return GCullFlags.VISIBLE;
        }

        /// <summary>
        /// Test if any point lies behind a plane
        /// The p[0-7] points represent the 8 corners of a AABB
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="p5"></param>
        /// <param name="p6"></param>
        /// <param name="p7"></param>
        /// <returns></returns>
        public static bool IsBehindPlaneAABB(ref Plane plane, ref Vector3 p0, ref Vector3 p1, ref Vector3 p2, ref Vector3 p3,
            ref Vector3 p4, ref Vector3 p5, ref Vector3 p6, ref Vector3 p7)
        {
            if (plane.GetSide(p0) == true) return false;
            if (plane.GetSide(p1) == true) return false;
            if (plane.GetSide(p2) == true) return false;
            if (plane.GetSide(p3) == true) return false;
            if (plane.GetSide(p4) == true) return false;
            if (plane.GetSide(p5) == true) return false;
            if (plane.GetSide(p6) == true) return false;
            if (plane.GetSide(p7) == true) return false;

            return true;
        }

        public static bool IsPointInsideFrustum(ref Plane plane0, ref Plane plane1, ref Plane plane2, ref Plane plane3, ref Plane plane4, ref Plane plane5, ref Vector3 point)
        {
            if (!plane0.GetSide(point))
                return false;
            if (!plane1.GetSide(point))
                return false;
            if (!plane2.GetSide(point))
                return false;
            if (!plane3.GetSide(point))
                return false;
            if (!plane4.GetSide(point))
                return false;
            if (!plane5.GetSide(point))
                return false;

            return true;
        }
    }
}
