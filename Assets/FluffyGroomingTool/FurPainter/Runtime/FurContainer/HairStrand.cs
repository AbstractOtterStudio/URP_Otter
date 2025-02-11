using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class HairStrand {
        public Vector4 bend;
        public Vector3 scaleMatrix;
        public Matrix4x4 rootAndOrientationMatrix;
        public Vector2 uv;
        public Vector2 uvOffset;
        public Vector3 triangles;
        public Vector3 barycentricCoordinates;
        public float windContribution;
        public int clumpIndices = -1;
        public float clumpMask;
        public Vector4 twist;
        public Vector4 overrideColor;

        public override string ToString() {
            return
                $"{nameof(bend)}: {bend}, {nameof(scaleMatrix)}: {scaleMatrix}, {nameof(rootAndOrientationMatrix)}: {rootAndOrientationMatrix}, {nameof(uv)}: {uv}, {nameof(uvOffset)}: {uvOffset}, {nameof(triangles)}: {triangles}, {nameof(barycentricCoordinates)}: {barycentricCoordinates}, {nameof(windContribution)}: {windContribution}";
        }

        public HairStrandStruct convertToStruct() {
            return new HairStrandStruct() {
                bend = bend,
                scaleMatrix = new Matrix4x4 {m00 = scaleMatrix.x, m11 = scaleMatrix.y, m22 = scaleMatrix.z, m33 = 1},
                rootAndOrientationMatrix = rootAndOrientationMatrix,
                uv = uv,
                uvOffset = uvOffset,
                triangles = triangles,
                barycentricCoordinates = barycentricCoordinates,
                windContribution = windContribution,
                clumpIndices = clumpIndices,
                clumpMask = clumpMask,
                twist = twist,
                overrideColor = overrideColor
            };
        }
    }

    public struct HairStrandStruct {
        public Vector4 bend;
        public Matrix4x4 scaleMatrix;
        public Matrix4x4 rootAndOrientationMatrix;
        public Vector2 uv;
        public Vector2 uvOffset;
        public Vector3 triangles;
        public Vector3 barycentricCoordinates;
        public float windContribution;
        public int clumpIndices;
        public float clumpMask;
        public Vector4 twist;
        public Vector4 overrideColor;

        public static int size() {
            return
                sizeof(float) * 4 + // bend
                sizeof(float) * 4 * 4 + // scaleMatrix   
                sizeof(float) * 4 * 4 + // rootAndOrientationMatrix   
                sizeof(float) * 2 + // uv
                sizeof(float) * 2 + // uvOffset 
                sizeof(float) * 3 + // triangles 
                sizeof(float) * 3 + // BarycentricCoordinate  
                sizeof(float) + // windContribution
                sizeof(int) + // clumpIndices
                sizeof(float) + //clumpMask
                sizeof(float) * 4 + //twist
                sizeof(float) * 4; //OverrideColor  
        }

        public HairStrand convertToObject() {
            return new HairStrand() {
                bend = bend,
                scaleMatrix = new Vector3(scaleMatrix.m00, scaleMatrix.m11, scaleMatrix.m22),
                rootAndOrientationMatrix = rootAndOrientationMatrix,
                uv = uv,
                uvOffset = uvOffset,
                triangles = triangles,
                barycentricCoordinates = barycentricCoordinates,
                windContribution = windContribution,
                clumpIndices = clumpIndices,
                clumpMask = clumpMask,
                twist = twist,
                overrideColor = overrideColor
            };
        }
    }
}