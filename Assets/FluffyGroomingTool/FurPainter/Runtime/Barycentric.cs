using FluffyGroomingTool;
using UnityEngine;

namespace FluffyGroomingTool {
    public class Barycentric {
        public float u;
        public float v;
        public float w;

        internal PointOnMesh pointOnMesh;

        internal Barycentric(Vector3 aV1, Vector3 aV2, Vector3 aV3, Vector3 aP) {
            Vector3 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
            var aLen = a.x * a.x + a.y * a.y + a.z * a.z;
            var bLen = b.x * b.x + b.y * b.y + b.z * b.z;
            var ab = a.x * b.x + a.y * b.y + a.z * b.z;
            var ac = a.x * c.x + a.y * c.y + a.z * c.z;
            var bc = b.x * c.x + b.y * c.y + b.z * c.z;
            var d = aLen * bLen - ab * ab;
            u = (aLen * bc - ab * ac) / d;
            v = (bLen * ac - ab * bc) / d;
            w = 1.0f - u - v;
            
            if (float.IsNaN(u) || float.IsNaN(v)|| float.IsNaN(w)) {
                u = 0.5f;
                v = 0.5f;
                w = 0.5f;
            }
        }

       public Vector3 getCoords() {
            return new Vector3(u, v, w);
        }

        public Vector2 interpolatedUv(Vector2[] uvs) {
            return interpolate(
                uvs[pointOnMesh.triangleIndex1],
                uvs[pointOnMesh.triangleIndex2],
                uvs[pointOnMesh.triangleIndex3]
            );
        }


        private Vector2 interpolate(Vector2 v1, Vector2 v2, Vector2 v3) {
            return v1 * u + v2 * v + v3 * w;
        }

        public static Vector3 interpolateV3(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 barycentricCoordinate) {
            return v1 * barycentricCoordinate.x + v2 * barycentricCoordinate.y + v3 * barycentricCoordinate.z;
        }
        public static Vector4 interpolateV4(Vector4 v1, Vector4 v2, Vector4 v3, Vector3 barycentricCoordinate) {
            return v1 * barycentricCoordinate.x + v2 * barycentricCoordinate.y + v3 * barycentricCoordinate.z; 
        }
        public static Vector2 interpolateV2(Vector2 v1, Vector2 v2, Vector2 v3, Vector3 barycentricCoordinate) {
            return v1 * barycentricCoordinate.x + v2 * barycentricCoordinate.y + v3 * barycentricCoordinate.z;
        }
    }
}

public static class BaryCentricExtensions {
    public static Barycentric creatBaryCentricMeshCoordinates(
        this PointOnMesh rPointOnMesh
    ) {
        return new Barycentric(
            rPointOnMesh.vertex1,
            rPointOnMesh.vertex2,
            rPointOnMesh.vertex3,
            rPointOnMesh.pos) {
            pointOnMesh = rPointOnMesh
        };
    }
}