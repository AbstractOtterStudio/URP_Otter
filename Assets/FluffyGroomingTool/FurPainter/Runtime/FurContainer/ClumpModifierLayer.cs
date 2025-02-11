using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [Serializable]
    public class ClumpModifierLayer : ICloneable {
        [SerializeField] public HairStrand[] clumps = new HairStrand[0];
        public AnimationCurve attractionCurve;
        public ComputeBuffer clumpsBuffer;
        public ComputeBuffer clumpsPositionBuffer;
        public ComputeBuffer clumpAttractionBuffer;

        public void recreateClumpBuffer() {
            if (clumps.Length > 0) {
                clumpsBuffer?.Dispose();
                clumpsBuffer = new ComputeBuffer(clumps.Length, HairStrandStruct.size());
                var clumpsStructs = clumps.ToList().Select(it => it.convertToStruct()).ToArray();
                clumpsBuffer.SetData(clumpsStructs);
                disposePositionAndAttractionBuffers();
            }
        }

        private void disposePositionAndAttractionBuffers() {
            clumpsPositionBuffer?.Dispose();
            clumpsPositionBuffer = null;
            clumpAttractionBuffer?.Dispose();
            clumpAttractionBuffer = null;
        }

        public void dispatchParentClumpKernel(ComputeShader cs, int kernel, FurRenderer renderer, CardMeshProperties cardMeshProperties) {
            if (clumpsBuffer != null) {
                var clumpYCoordinates = cardMeshProperties.getCardMeshVerticesY();
                var clumpsPositionBufferCount = clumpYCoordinates * clumpsBuffer.count;
                if (clumpsPositionBuffer == null)
                    clumpsPositionBuffer = new ComputeBuffer(clumpsPositionBufferCount, sizeof(float) * 4, ComputeBufferType.Default);
                cs.DisableKeyword("IS_CHILD_CLUMP");
                cs.SetFloat("clumpPointsCount", clumpsPositionBufferCount);
                cs.SetBuffer(kernel, "sourceMesh", renderer.meshBaker.bakedMesh);
                cs.SetBuffer(kernel, "clumpPointsPosition", clumpsPositionBuffer);
                cs.SetBuffer(kernel, "clumpsBuffer", clumpsBuffer);
                cs.SetInt("clumpYCoordinates", clumpYCoordinates);
                cs.Dispatch(kernel, clumpsPositionBufferCount.toCsGroups(), 1, 1);
            }
        }

        public void dispatchChildClumpKernel(ClumpModifierLayer parentClumpModifier, ComputeShader cs, int kernel, FurRenderer renderer,
            CardMeshProperties cardMeshProperties) {
            if (clumpsBuffer != null) {
                cs.EnableKeyword("IS_CHILD_CLUMP");
                var clumpYCoordinates = cardMeshProperties.getCardMeshVerticesY();
                var clumpsPositionBufferCount = clumpYCoordinates * clumpsBuffer.count;
                if (clumpsPositionBuffer == null)
                    clumpsPositionBuffer = new ComputeBuffer(clumpsPositionBufferCount, sizeof(float) * 4, ComputeBufferType.Default);
                cs.SetBool("isChildClumpKernel", true);
                cs.SetInt("clumpYCoordinates", clumpYCoordinates);
                cs.SetFloat("clumpPointsCount", clumpsPositionBufferCount);
                cs.SetBuffer(kernel, "clumpPointsPosition", clumpsPositionBuffer);
                cs.SetBuffer(kernel, "parentClumpPointsPosition", parentClumpModifier.clumpsPositionBuffer);
                cs.SetBuffer(kernel, "clumpsBuffer", clumpsBuffer);
                cs.SetBuffer(kernel, "clumpAttractionCurve", parentClumpModifier.clumpAttractionBuffer);
                cs.Dispatch(kernel, clumpsPositionBufferCount.toCsGroups(), 1, 1);
            }
        }

        public void dispose() {
            disposePositionAndAttractionBuffers();
            clumpsBuffer?.Dispose();
            clumpsBuffer = null;
        }

        public void createClumpAttractionBuffer(int attractionPointsCount) {
            clumpAttractionBuffer = new ComputeBuffer(attractionPointsCount, sizeof(float));
        }

        public void updateClumpAttractionBuffer() {
            if (clumpAttractionBuffer != null) {
                var count = clumpAttractionBuffer.count;
                var data = new float[count];
                for (int i = 0; i < count; i++) {
                    data[i] = attractionCurve.Evaluate((float) i / count);
                }

                clumpAttractionBuffer.SetData(data);
            }
        }

        public void copyFromComputeBufferToNativeObject() {
            if (clumpsBuffer != null) AsyncGPUReadback.Request(clumpsBuffer, OnCompleteReadback);
        }


        void OnCompleteReadback(AsyncGPUReadbackRequest request) {
            if (request.hasError) {
                Debug.Log("GPU readback error detected for Clumps in FurContainer.");
                return;
            }

            var hairStrandStructs = request.ToPersistentArray<HairStrandStruct>();
            new Thread(() => {
                clumps = hairStrandStructs.Select(it => it.convertToObject()).ToArray();
                hairStrandStructs.Dispose();
            }).Start();
        }

        void convertNativeArrayToStructs() { }

        public object Clone() {
            return new ClumpModifierLayer {
                clumps = clumps,
                attractionCurve = new AnimationCurve(attractionCurve.keys)
            };
        }
    }
}