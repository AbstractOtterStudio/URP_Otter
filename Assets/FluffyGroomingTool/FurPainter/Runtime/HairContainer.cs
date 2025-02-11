using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FluffyGroomingTool {
    [PreferBinarySerialization]
    public class HairContainer : ScriptableObject {
        [SerializeField, HideInInspector] public HairStrandPoint[] hairStrandPoints;
        public int pointsPerStrand;
        [SerializeField, HideInInspector] public int id = -1324198676 + Guid.NewGuid().GetHashCode();
        [SerializeField] public float objectScaleAtSkinning = 1;

        [SerializeField] public AnimationCurve shapeCurve = new AnimationCurve(
            new Keyframe(0, 1f, -0.007165052f, -0.007165052f, 0, 0.6583334f),
            new Keyframe(1f, 0f, -16.49596f, -16.49596f, 0.05833334f, 0)
        );

        [Range(0.00001f, 0.01f)] public float strandsWidth = 0.0005f;
        [SerializeField] public bool isSkinned = true;

        public ComputeBuffer createShapeBuffer(ComputeBuffer strandShapeBuffer) {
            float[] shapeWidthMultipliers = new float[pointsPerStrand];
            for (float i = 0; i < pointsPerStrand; i++) {
                shapeWidthMultipliers[(int)i] = shapeCurve.Evaluate(i / (pointsPerStrand - 1));
            }

            strandShapeBuffer ??= new ComputeBuffer(pointsPerStrand, sizeof(float));
            strandShapeBuffer.SetData(shapeWidthMultipliers);
            return strandShapeBuffer;
        }

        public void regenerateID() {
            id = -1324198676 + Guid.NewGuid().GetHashCode();
        }

        static bool isInsideTriangle(Vector3 bc) {
            return bc.x >= 0 && bc.y >= 0 && bc.x + bc.y <= 1;
        }


        public static HairContainer createFromAlembicAndSkinCpu(
            Vector3[] curvePoints,
            int strandPointsCount,
            GameObject skinToGameObject
        ) {
            var bindToMeshCopy = getMesh(skinToGameObject, out var errorMessage);
            var strands = curvePoints.split(strandPointsCount);
            var hairStrandPoints = new List<HairStrandPoint>();
            var bindToNormals = bindToMeshCopy.normals;
            var bindToVertices = bindToMeshCopy.vertices;
            var bindToTangents = bindToMeshCopy.tangents;
            var bindToTriangles = bindToMeshCopy.triangles.split(3).ToArray();
            var bindToUvs = bindToMeshCopy.uv;

            var strandsCount = strands.Count;

            var lossyScaleX = skinToGameObject.transform.localToWorldMatrix.inverse.lossyScale.x;
            var missedStrands = 0;
            for (var strandIndex = 0; strandIndex < strandsCount; strandIndex++) {
                var strandPoints = strands[strandIndex];
                bool wasHit = false;
                for (var triIndex = 0; triIndex < bindToTriangles.Length; triIndex++) {
                    var triangle = bindToTriangles[triIndex];
                    var triIndex1 = triangle[0];
                    var triIndex2 = triangle[1];
                    var triIndex3 = triangle[2];

                    var vert1 = bindToVertices[triIndex1];
                    var vert2 = bindToVertices[triIndex2];
                    var vert3 = bindToVertices[triIndex3];


                    var rootCurvePoint = strandPoints[0];
                    var barycentricCoordinate = new Barycentric(vert1, vert2, vert3, rootCurvePoint).getCoords();
                    var baryCentricPosition = Barycentric.interpolateV3(vert1, vert2, vert3, barycentricCoordinate);
                    var pointRootIndex = strandIndex * strandPointsCount;


                    if (isInsideTriangle(barycentricCoordinate) &&
                        Vector3.Distance(baryCentricPosition, rootCurvePoint) < 0.005f) {
                        Vector3 normal1 = bindToNormals[triIndex1];
                        Vector3 normal2 = bindToNormals[triIndex2];
                        Vector3 normal3 = bindToNormals[triIndex3];

                        var tangent1 = bindToTangents[triIndex1];
                        var tangent2 = bindToTangents[triIndex2];
                        var tangent3 = bindToTangents[triIndex3];
                        Vector3 barycentricNormal = Barycentric.interpolateV3(normal1, normal2, normal3, barycentricCoordinate);
                        Vector2 uv = Barycentric.interpolateV2(
                            bindToUvs[triIndex1],
                            bindToUvs[triIndex2],
                            bindToUvs[triIndex3],
                            barycentricCoordinate
                        );

                        Vector4 barycentricTangent = Barycentric.interpolateV4(tangent1, tangent2, tangent3, barycentricCoordinate);
                        Quaternion normalTangentRotation = Quaternion.LookRotation(barycentricTangent, barycentricNormal);

                        Quaternion normalTangentRotationInverse = Quaternion.Inverse(normalTangentRotation);
                        Matrix4x4 inverseRotationMatrix = Matrix4x4.TRS(Vector3.zero, normalTangentRotationInverse, Vector3.one);

                        for (int straindPointIndex = pointRootIndex; straindPointIndex < pointRootIndex + strandPointsCount; straindPointIndex++) {
                            Vector3 localStrandPoint = curvePoints[straindPointIndex];
                            float originalDistance = Vector3.Distance(baryCentricPosition, localStrandPoint);

                            localStrandPoint -= baryCentricPosition;
                            localStrandPoint = inverseRotationMatrix.MultiplyPoint(localStrandPoint);


                            HairStrandPoint p = new HairStrandPoint() {
                                barycentricCoordinate = barycentricCoordinate,
                                triangleIndices = new(triIndex1, triIndex2, triIndex3),
                                rotationDiffFromNormal = localStrandPoint * lossyScaleX,
                                distanceToRoot = originalDistance * lossyScaleX,
                                uv = uv
                            };
                            hairStrandPoints.Add(p);
                        }

                        wasHit = true;
                    }
                }

                if (!wasHit) {
                    missedStrands++;
                }
            }

            Debug.Log("Missed strands count: " + missedStrands);
            var hairContainer = CreateInstance<HairContainer>();
            hairContainer.hairStrandPoints = hairStrandPoints.ToArray();
            hairContainer.pointsPerStrand = strandPointsCount;
            hairContainer.objectScaleAtSkinning =
                skinToGameObject.transform.lossyScale.x; //For now we only support uniform scaling.

            DestroyImmediate(bindToMeshCopy);
            return hairContainer;
        }

        private static Mesh getMesh(GameObject skinToGameObject, out string errorMessage) {
            var skinnedMeshRenderer = skinToGameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null) {
                var mesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(mesh, true);
                var oldVerts = mesh.vertices;
                var oldNormals = mesh.normals;
                var oldTangents = mesh.tangents;

                if (isMeshInvalid(out errorMessage, oldNormals, oldTangents)) return null;

                var localToWorld = skinToGameObject.transform.localToWorldMatrix;
                mesh.vertices = (from vert in oldVerts select localToWorld.MultiplyPoint(vert)).ToArray();
                mesh.normals = (from norm in oldNormals select localToWorld.MultiplyVector(norm.normalized).normalized).ToArray();
                mesh.tangents = (from tang in oldTangents
                    let multiplyVector = localToWorld.MultiplyVector(tang.normalized).normalized
                    select new Vector4(multiplyVector.x, multiplyVector.y, multiplyVector.z, tang.w)).ToArray();

                return mesh;
            } else {
                var mesh = skinToGameObject.GetComponent<MeshFilter>().sharedMesh;
                var newMesh = new Mesh();
                var newVerts = new List<Vector3>();
                var newNormals = new List<Vector3>();
                var oldVerts = mesh.vertices;
                var oldNormals = mesh.normals;
                var localToWorld = skinToGameObject.transform.localToWorldMatrix;
                for (int i = 0; i < oldVerts.Length; i++) {
                    newVerts.Add(localToWorld.MultiplyPoint(oldVerts[i]));
                    newNormals.Add(localToWorld.MultiplyVector(oldNormals[i]).normalized);
                }

                if (isMeshInvalid(out errorMessage, oldNormals, mesh.tangents)) return null;
                newMesh.vertices = newVerts.ToArray();
                newMesh.normals = newNormals.ToArray();
                newMesh.tangents = mesh.tangents;
                newMesh.indexFormat = mesh.indexFormat;
                newMesh.triangles = mesh.triangles;
                newMesh.uv = mesh.uv;
                return newMesh;
            }
        }

        private static bool isMeshInvalid(out string errorMessage, Vector3[] oldNormals, Vector4[] oldTangents) {
            errorMessage = "";
            
            if (oldNormals.Length == 0 && oldTangents.Length == 0) {
                errorMessage = "Uh oh. The mesh you are trying to skin the hairs to does not have Normals and Tangents.";
                return true;
            }
            if (oldNormals.Length == 0) {
                errorMessage = "Uh oh. The mesh you are trying to skin the hairs to does not have Normals.";
                return true;
            }

            if (oldTangents.Length == 0) {
                errorMessage = "Uh oh. The mesh you are trying to skin the hairs to does not have Tangents.";
                return true;
            }

            return false;
        }

        public static HairContainer createFromAlembicAndSkinGpu(
            Vector3[] curvePoints,
            int strandPointsCount,
            GameObject skinToGameObject,
            out string errorMessage
        ) {
            var bindToMeshCopy = getMesh(skinToGameObject, out errorMessage);
            if (bindToMeshCopy == null) return null;

            var meshBaker = new MeshBaker(bindToMeshCopy, Instantiate(Resources.Load<ComputeShader>(ShaderID.MESH_BAKER_CS_NAME)));
            var meshBakerBakedMesh = meshBaker.bakedMesh;
            var compute = Resources.Load<ComputeShader>("AlembicSkinning");
            var hairStrandPointsBuffer = new ComputeBuffer(curvePoints.Length, Marshal.SizeOf<HairStrandPointStruct>());
            compute.SetBuffer(0, "hairStrandPoints", hairStrandPointsBuffer);
            var skinToTransform = skinToGameObject.transform;
            compute.SetFloat("inverseScale", skinToTransform.localToWorldMatrix.inverse.lossyScale.x);

            var pointsBuffer = new ComputeBuffer(curvePoints.Length, sizeof(float) * 3);
            pointsBuffer.SetData(curvePoints);
            compute.SetBuffer(0, "curvePoints", pointsBuffer);

            compute.SetInt("strandPointsCount", strandPointsCount);

            compute.SetBuffer(0, ShaderID.SOURCE_MESH, meshBakerBakedMesh);
            compute.SetBuffer(0, "meshIndexBuffer", meshBaker.getIndexBuffer());

            compute.SetInt("maxX", curvePoints.Length);
            compute.SetInt("maxY", meshBaker.sourceMesh.triangles.Length);

            var uvBuffer = new ComputeBuffer(meshBaker.sourceMesh.uv.Length, sizeof(float) * 2);
            uvBuffer.SetData(meshBaker.sourceMesh.uv);
            compute.SetBuffer(0, "uvBuffer", uvBuffer);

            compute.Dispatch(
                0,
                numGroups((int)MathF.Ceiling((float)curvePoints.Length / strandPointsCount), 256),
                1,
                1
            );
            var hairStrandPoints = new HairStrandPointStruct[curvePoints.Length];
            hairStrandPointsBuffer.GetData(hairStrandPoints);

            var hairContainerHairStrandPoints =
                hairStrandPoints.ToList().Where(it => it.triangleIndices != Vector3.zero).Select(it => it.convertToSObject()).ToArray();
            if (hairContainerHairStrandPoints.Length == 0) {
                errorMessage = "No hairs where skinned to the object. Please make sure the hair curves are aligned properly on the mesh surface.";
                return null;
            }

            var hairContainer = CreateInstance<HairContainer>();
            hairContainer.hairStrandPoints = hairContainerHairStrandPoints;
            hairContainer.pointsPerStrand = strandPointsCount;

            meshBaker.dispose();
            hairStrandPointsBuffer.Dispose();
            pointsBuffer.Dispose();
            uvBuffer.Dispose();
            hairContainer.objectScaleAtSkinning = skinToTransform.lossyScale.x; //For now we only support uniform scaling.
            return hairContainer;
        }

        private static int numGroups(int totalThreads, int groupSize) {
            return (totalThreads + (groupSize - 1)) / groupSize;
        }

        public static HairContainer createFromAlembicWithoutSkinning(
            Vector3[] positions,
            int strandPointsCount,
            List<Vector2> uvs
        ) {
            var hairContainer = CreateInstance<HairContainer>();
            hairContainer.hairStrandPoints = positions.ToList().Select((pos, index) => new HairStrandPoint {
                barycentricCoordinate = pos,
                triangleIndices = Vector3.zero,
                rotationDiffFromNormal = Vector3.zero,
                distanceToRoot = 0,
                uv = index < uvs.Count - 1 ? uvs[index] : Vector2.zero
            }).ToArray();
            hairContainer.pointsPerStrand = strandPointsCount;
            hairContainer.isSkinned = false;
            return hairContainer;
        }
    }

    public struct HairStrandPointStruct {
        public Vector3 barycentricCoordinate;
        public Vector3 triangleIndices;
        public Vector3 rotationDiffFromNormal;
        public float distanceToRoot;
        public Vector2 uv;

        public HairStrandPoint convertToSObject() {
            return new HairStrandPoint {
                barycentricCoordinate = barycentricCoordinate,
                triangleIndices = triangleIndices,
                rotationDiffFromNormal = rotationDiffFromNormal,
                distanceToRoot = distanceToRoot,
                uv = uv
            };
        }
    }

    [Serializable]
    public struct HairStrandPoint {
        public Vector3 barycentricCoordinate;
        public Vector3 triangleIndices;
        public Vector3 rotationDiffFromNormal;
        public float distanceToRoot;
        public Vector2 uv;

        public HairStrandPointStruct convertToStruct() {
            return new HairStrandPointStruct() {
                barycentricCoordinate = barycentricCoordinate,
                triangleIndices = triangleIndices,
                rotationDiffFromNormal = rotationDiffFromNormal,
                distanceToRoot = distanceToRoot,
                uv = uv
            };
        }
    }
}