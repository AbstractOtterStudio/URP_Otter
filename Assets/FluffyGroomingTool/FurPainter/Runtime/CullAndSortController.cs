using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FluffyGroomingTool {
    public class CullAndSortController {
        private ComputeShader bitonicSortComputeShader;
        private ComputeShader cullAndDistanceComputeShader;
        private GraphicsBuffer indexBuffer;
        private readonly int distanceAndVisibility;
        private readonly int assembleIndicesKernel;
        private readonly int frustumCullingAndLodOnlyKernel;

        private readonly int assembleVisibleTrianglesIndices;
        private int currentLodIndex = -1;
        private readonly ComputeBuffer triangles;
        private readonly ComputeBuffer visibleTriangles;
        private readonly BitonicMergeSort bitonicSorter;
        private readonly ComputeBuffer sortedKeys;
        private GraphicsBuffer.IndirectDrawIndexedArgs[] renderMeshData;
        public GraphicsBuffer RenderMeshArguments { get; }

        private FurRendererSettings settings;

        public CullAndSortController(int meshBufferStride, GraphicsBuffer indexBuffer, GraphicsBuffer furMeshBuffer, int trianglesCount,
            FurRendererSettings settings) {
            this.indexBuffer = indexBuffer;
            RenderMeshArguments = createRenderMeshArguments();
            this.settings = settings;

            cullAndDistanceComputeShader = Object.Instantiate(Resources.Load<ComputeShader>("CullingComputeShader"));
            bitonicSortComputeShader = Object.Instantiate(Resources.Load<ComputeShader>("BitonicSort"));

            triangles = new ComputeBuffer(trianglesCount, sizeof(int) * 3 + sizeof(float) + sizeof(int));
            visibleTriangles = new ComputeBuffer(trianglesCount, sizeof(int) * 3 + sizeof(float) + sizeof(int), ComputeBufferType.Append);

            distanceAndVisibility = cullAndDistanceComputeShader.FindKernel("DistanceAndVisibility");
            var initializeKernel = cullAndDistanceComputeShader.FindKernel("Initialize");
            assembleIndicesKernel = cullAndDistanceComputeShader.FindKernel("AssembleIndices");
            assembleVisibleTrianglesIndices = cullAndDistanceComputeShader.FindKernel("AssembleVisibleTrianglesIndices");
            frustumCullingAndLodOnlyKernel = cullAndDistanceComputeShader.FindKernel("FrustomCullingAndLodOnly");


            cullAndDistanceComputeShader.SetBuffer(initializeKernel, ShaderID.MESH_INDEX_BUFFER, indexBuffer);
            cullAndDistanceComputeShader.SetBuffer(initializeKernel, ShaderID.TRIANGLES, triangles);

            cullAndDistanceComputeShader.SetBuffer(assembleIndicesKernel, ShaderID.MESH_INDEX_BUFFER, indexBuffer);
            cullAndDistanceComputeShader.SetBuffer(assembleIndicesKernel, ShaderID.TRIANGLES, triangles);
            cullAndDistanceComputeShader.SetBuffer(assembleIndicesKernel, ShaderID.VISIBLE_TRIANGLES, visibleTriangles);
            cullAndDistanceComputeShader.SetBuffer(assembleIndicesKernel, ShaderID.FUR_MESH_BUFFER, furMeshBuffer);

            cullAndDistanceComputeShader.SetBuffer(assembleVisibleTrianglesIndices, ShaderID.MESH_INDEX_BUFFER, indexBuffer);
            cullAndDistanceComputeShader.SetBuffer(assembleVisibleTrianglesIndices, ShaderID.VISIBLE_TRIANGLES_READ, visibleTriangles);

            cullAndDistanceComputeShader.SetBuffer(distanceAndVisibility, ShaderID.FUR_MESH_BUFFER, furMeshBuffer);
            cullAndDistanceComputeShader.SetBuffer(distanceAndVisibility, ShaderID.TRIANGLES, triangles);
            cullAndDistanceComputeShader.SetBuffer(distanceAndVisibility, ShaderID.VISIBLE_TRIANGLES, visibleTriangles);

            cullAndDistanceComputeShader.SetBuffer(frustumCullingAndLodOnlyKernel, ShaderID.TRIANGLES, triangles);
            cullAndDistanceComputeShader.SetBuffer(frustumCullingAndLodOnlyKernel, ShaderID.FUR_MESH_BUFFER, furMeshBuffer);
            cullAndDistanceComputeShader.SetBuffer(frustumCullingAndLodOnlyKernel, ShaderID.MESH_INDEX_BUFFER, indexBuffer);

            cullAndDistanceComputeShader.SetInt(ShaderID.VERTEX_BUFFER_STRIDE, meshBufferStride);
            cullAndDistanceComputeShader.SetFloat(ShaderID.IS_FLIPPED_Y, SystemInfo.graphicsUVStartsAtTop ? 1 : -1);

            cullAndDistanceComputeShader.Dispatch(initializeKernel, trianglesCount.toCsGroups(), 1, 1);

            var powerOf2SortIndices = nextPowerOf2(trianglesCount);
            sortedKeys = new ComputeBuffer(powerOf2SortIndices, sizeof(uint));
            bitonicSorter = new BitonicMergeSort(bitonicSortComputeShader);
            bitonicSorter.inizialize(sortedKeys);
        }

        private GraphicsBuffer createRenderMeshArguments() {
            var args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            renderMeshData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            renderMeshData[0].indexCountPerInstance = (uint) indexBuffer.count;
            renderMeshData[0].instanceCount = 1;
            args.SetData(renderMeshData);
            return args;
        }

        public int getCurrentLodIndex() {
            return Mathf.Max(currentLodIndex, 0);
        }

        public static int nextPowerOf2(int n) {
            int count = 0;
            if (n > 0 && (n & (n - 1)) == 0)
                return n;

            while (n != 0) {
                n >>= 1;
                count += 1;
            }

            return 1 << count;
        }

        public void update(Camera mainCamera, FurContainer furContainer, Vector3 objectPosition) {
            IsCulled = false;
            if (!settings.IsLodEnabled && currentLodIndex != 0) {
                setLodIndex(0, furContainer);
            }

            if (mainCamera == null && Application.isPlaying) {
                Debug.Log("Fur LOD will not work unless you assign the lodCamera.");
                return;
            }

            setupLod(mainCamera, furContainer, objectPosition);
            Stopwatch sw = Stopwatch.StartNew();
            sendCommonValuesToShader(mainCamera);

            if (isFrustumAndLodOnly()) {
                dispatchFrustumAndLodOnlyKernel();
            }
            else if (settings.isAlphaSortingEnabled) {
                dispatchSortAndCullKernels();
            }

            sw.Stop();
        }

        private bool isFrustumAndLodOnly() {
            return settings.IsFrustumCullingEnabled && settings.IsLodEnabled && !settings.isOcclusionCullingEnabled &&
                   !settings.isAlphaSortingEnabled;
        }

        private bool isLodOnly() {
            return !settings.IsFrustumCullingEnabled && settings.IsLodEnabled && !settings.isOcclusionCullingEnabled &&
                   !settings.isAlphaSortingEnabled;
        }

        private void sendCommonValuesToShader(Camera mainCamera) {
            cullAndDistanceComputeShader.SetMatrix(ShaderID.UNITY_MATRIX_MVP, getMVPMatrix(mainCamera));
            cullAndDistanceComputeShader.SetVector(ShaderID.WORLD_SPACE_CAMERA_POSITION, mainCamera.transform.position);
            cullAndDistanceComputeShader.SetInt(ShaderID.IS_FRUSTUM_CULLING_ENABLED, settings.isFrustumCullingEnabled ? 1 : 0);
            cullAndDistanceComputeShader.SetFloat(ShaderID.CAMERA_FAR_CLIP_PLANE, mainCamera.farClipPlane);
        }

        private void dispatchSortAndCullKernels() {
            visibleTriangles.SetCounterValue(0);
            cullAndDistanceComputeShader.SetInt(ShaderID.TRIANGLES_COUNT, triangles.count);
            cullAndDistanceComputeShader.Dispatch(distanceAndVisibility, triangles.count.toCsGroups(), 1, 1);
            var numberOfVisibleTriangles = updateArgumentsBuffer();

            cullAndDistanceComputeShader.SetInt(ShaderID.NUM_VISIBLE_TRIANGLES_COUNT, numberOfVisibleTriangles);
            if (settings.isAlphaSortingEnabled && numberOfVisibleTriangles > 0) {
                bitonicSorter.sort(sortedKeys, triangles);
                cullAndDistanceComputeShader.SetBuffer(assembleIndicesKernel, ShaderID.KEYS, sortedKeys);
                cullAndDistanceComputeShader.Dispatch(assembleIndicesKernel, numberOfVisibleTriangles.toCsGroups(), 1, 1);
            }
            else if (!settings.isAlphaSortingEnabled && numberOfVisibleTriangles > 0) {
                cullAndDistanceComputeShader.Dispatch(assembleVisibleTrianglesIndices, numberOfVisibleTriangles.toCsGroups(), 1, 1);
            }
        }

        public void dispatchFrustumAndLodOnlyKernel() {
            renderMeshData[0].indexCountPerInstance = (uint) (triangles.count * 3);
            RenderMeshArguments.SetData(renderMeshData);
            cullAndDistanceComputeShader.SetInt(ShaderID.TRIANGLES_COUNT, triangles.count);
            cullAndDistanceComputeShader.Dispatch(frustumCullingAndLodOnlyKernel, triangles.count.toCsGroups(), 1, 1);
        }

        private int updateArgumentsBuffer() {
            GraphicsBuffer.CopyCount(visibleTriangles, RenderMeshArguments, 0);
            RenderMeshArguments.GetData(renderMeshData);
            var visibleTrianglesCount = renderMeshData[0].indexCountPerInstance;
            renderMeshData[0].indexCountPerInstance = visibleTrianglesCount * 3;
            RenderMeshArguments.SetData(renderMeshData);
            return (int) visibleTrianglesCount;
        }

        public bool IsCulled { get; set; }

        private void setupLod(Camera mainCamera, FurContainer furContainer, Vector3 objectPosition) {
            if (settings.IsLodEnabled) {
                var cameraPosition = mainCamera.transform.position;
                var objectDistanceToCamera = Vector3.Distance(cameraPosition, objectPosition);
                var lod1StartDistance = furContainer.furLods[1].startDistance;
                var lod2StartDistance = furContainer.furLods[2].startDistance;

                if (objectDistanceToCamera > furContainer.culledDistance) {
                    IsCulled = true;
                }
                else if (objectDistanceToCamera > lod2StartDistance && currentLodIndex != 2) {
                    setLodIndex(2, furContainer);
                }
                else if (objectDistanceToCamera < lod2StartDistance && objectDistanceToCamera > lod1StartDistance && currentLodIndex != 1) {
                    setLodIndex(1, furContainer);
                }
                else if (objectDistanceToCamera < lod1StartDistance && currentLodIndex != 0) {
                    setLodIndex(0, furContainer);
                }
            }
            else {
                cullAndDistanceComputeShader.SetInt(ShaderID.LOD_SKIP_STRANDS_COUNT, 0);
            }
        }

        private Matrix4x4 getMVPMatrix(Camera mainCamera) {
            var v = mainCamera.worldToCameraMatrix;
            var p = mainCamera.projectionMatrix;
            return p * v;
        }

        public void dispose() {
            indexBuffer?.Release();
            triangles?.Dispose();
            visibleTriangles?.Dispose();
            RenderMeshArguments?.Dispose();
            sortedKeys?.Dispose();
        }

        public void setLodIndex(int index, FurContainer furContainer) {
            if (currentLodIndex != index) {
                currentLodIndex = index;
                var currentLod = furContainer.furLods[index];
                cullAndDistanceComputeShader.SetInt(ShaderID.LOD_SKIP_STRANDS_COUNT, currentLod.skipStrandsCount);

                if (isLodOnly()) {
                    dispatchFrustumAndLodOnlyKernel();
                }
            }
        }

        public string getCurrentLodName() {
            if (IsCulled) {
                return "Culled";
            }

            return currentLodIndex switch {
                0 => "LOD 1",
                1 => "LOD 2",
                _ => "LOD 3"
            };
        }
    }
}