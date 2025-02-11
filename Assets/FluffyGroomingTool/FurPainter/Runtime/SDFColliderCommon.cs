using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyGroomingTool {
    [Serializable]
    public class SDFColliderCommon {
        public ComputeShader computeShader;
        private ComputeBuffer sdf;
        private Renderer renderer;


        private int initializeSignedDistanceFieldKernel;
        private int constructSignedDistanceFieldKernel;
        private int finalizeSignedDistanceFieldKernel;
        private int collideWithVerletNodesKernel;

        private MeshBaker meshBaker;
        private VerletSimulationSettings settings;
        public bool debug;

        public SDFColliderCommon(Renderer renderer, MeshBaker meshBaker, VerletSimulationSettings settings) {
            computeShader = Object.Instantiate(Resources.Load<ComputeShader>("SDFColliderCompute"));
            initializeSignedDistanceFieldKernel = computeShader.FindKernel("InitializeSignedDistanceField");
            constructSignedDistanceFieldKernel = computeShader.FindKernel("ConstructSignedDistanceField");
            finalizeSignedDistanceFieldKernel = computeShader.FindKernel("FinalizeSignedDistanceField");
            collideWithVerletNodesKernel = computeShader.FindKernel("CollideWithVerletNodesKernel");
            this.renderer = renderer;
            this.meshBaker = meshBaker;
            this.settings = settings;
        }

        internal void createSDF(Transform transform, [CanBeNull] ComputeShader additionalCompute, int additionalKernel) {
            var triangleIndicesCount = meshBaker.indexBuffer.count;
            // initialize SDF grid using the associated model's bounding box
            var bounds = renderer.bounds;
            Vector3 bmin = bounds.min;
            Vector3 bmax = bounds.max;

            float bdiffX = (bmax.x - bmin.x);
            var cellSize = bdiffX / settings.sdfColliderResolution;
            int numExtraPaddingCells = (int) (settings.extraPaddingInCells * settings.sdfColliderResolution);
            var paddingCells = numExtraPaddingCells * cellSize;
            var paddingBoundary = new Vector3(paddingCells, paddingCells, paddingCells);

            var origin = bmin - paddingBoundary;

            bmin -= paddingBoundary;
            bmax += paddingBoundary;
            var bdiffY = bmax.y - bmin.y;
            var bdiffZ = bmax.z - bmin.z;
            var numCellsX = (int) ((bmax.x - bmin.x) / cellSize);
            var numCellsY = (int) ((bdiffY) / cellSize);
            var numCellsZ = (int) ((bdiffZ) / cellSize);
            var numTotalCells = (int) (settings.gridAllocationMultiplier * numCellsX * numCellsY * numCellsZ);

            sdf ??= new ComputeBuffer(numTotalCells, sizeof(uint), ComputeBufferType.Default);

            computeShader.SetInt("numTriangles", (int) (triangleIndicesCount / 3f));

            sendValuesToCompute(computeShader, initializeSignedDistanceFieldKernel, origin, cellSize, numCellsX, numCellsY, numCellsZ);

            //Dispatch CS  
            computeShader.Dispatch(initializeSignedDistanceFieldKernel, numTotalCells.toCsGroupsLarge(), 1, 1);

            computeShader.SetBuffer(constructSignedDistanceFieldKernel, "g_TrimeshVertexIndices", meshBaker.indexBuffer);
            computeShader.SetMatrix("rootMatrix", transform.localToWorldMatrix);

            computeShader.SetBuffer(constructSignedDistanceFieldKernel, "collMeshVertexPositions", meshBaker.bakedMesh);
            computeShader.SetBuffer(constructSignedDistanceFieldKernel, "g_SignedDistanceField", sdf);
            computeShader.Dispatch(constructSignedDistanceFieldKernel, ((int) (triangleIndicesCount / 3f)).toCsGroupsLarge(), 1, 1);

            computeShader.SetBuffer(finalizeSignedDistanceFieldKernel, "g_SignedDistanceField", sdf);
            computeShader.Dispatch(finalizeSignedDistanceFieldKernel, numTotalCells.toCsGroupsLarge(), 1, 1);
            if (additionalCompute != null) {
                additionalCompute.SetFloat("collisionMargin", settings.colliderSkinWidth);
                sendValuesToCompute(additionalCompute, additionalKernel, origin, cellSize, numCellsX, numCellsY, numCellsZ);
            }
        }

        private void sendValuesToCompute(ComputeShader compute, int kernel, Vector3 origin, float cellSize, int numCellsX, int numCellsY,
            int numCellsZ) {
            compute.SetVector("g_Origin", new Vector4(origin.x, origin.y, origin.z, 0));
            compute.SetFloat("g_CellSize", cellSize);
            compute.SetInt("g_NumCellsX", numCellsX);
            compute.SetInt("g_NumCellsY", numCellsY);
            compute.SetInt("g_NumCellsZ", numCellsZ);
            compute.SetBuffer(kernel, "g_SignedDistanceField", sdf);
            compute.SetBuffer(kernel, "g_SignedDistanceField_read_only", sdf);
        }

        public void dispose() {
            sdf?.Release();
            sdf = null;
        }

        public void collideWith(ComputeBuffer verletNodes, GraphicsBuffer furMeshBuffer, int vertexBufferStride) {
            if (Application.isPlaying && verletNodes != null && sdf != null) {
                var verletNodesCount = verletNodes.count;
                computeShader.SetInt("nodesCount", verletNodesCount);
                computeShader.SetBuffer(collideWithVerletNodesKernel, "g_SignedDistanceField", sdf);
                computeShader.SetBuffer(collideWithVerletNodesKernel, "g_SignedDistanceField_read_only", sdf);
                computeShader.SetBuffer(collideWithVerletNodesKernel, "verletNodes", verletNodes);
                computeShader.SetFloat("collisionMargin", settings.colliderSkinWidth);
                computeShader.SetInt(ShaderID.FUR_MESH_BUFFER_STRIDE, vertexBufferStride);
                computeShader.setKeywordEnabled("USE_FORWARD_COLLISION", settings.useForwardCollision);
                computeShader.SetBuffer(collideWithVerletNodesKernel, ShaderID.FUR_MESH_BUFFER, furMeshBuffer);
                computeShader.Dispatch(collideWithVerletNodesKernel, verletNodesCount.toCsGroupsLarge(), 1, 1);
            }
        }
    }
}