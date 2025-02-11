using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyGroomingTool {
    public class VerletSimulation {
        internal readonly ComputeShader compute;
        internal ComputeBuffer verletNodesBuffer, verletRestPositionsBuffer;
        private readonly int nodesCount;
        private readonly FurRendererSettings furRendererSettings;
        internal readonly int verletKernel;

        public VerletSimulation(int nodesCount, FurRendererSettings furRendererSettings) {
            this.nodesCount = nodesCount / 2;
            this.furRendererSettings = furRendererSettings;
            compute = Object.Instantiate(Resources.Load<ComputeShader>("VerletSimulation"));
            verletKernel = compute.FindKernel("StepAndSolve");
        }

        public void update(int strandsCount, int strandNodesCount, int layerNodeStartIndex, Vector3 worldSpaceCameraPos,
            VerletSimulationSettings simulationSettings, float normalPercent) {
            if (simulationSettings?.enableMovement == true && Application.isPlaying && verletNodesBuffer != null) {
                stepRelaxAndCollision(strandsCount, strandNodesCount, layerNodeStartIndex, worldSpaceCameraPos, simulationSettings, normalPercent);
            }
        }

        private void initBuffers() {
            if (verletNodesBuffer == null) {
                verletNodesBuffer = new ComputeBuffer(nodesCount, sizeof(float) * 15 + sizeof(int));
                verletRestPositionsBuffer = new ComputeBuffer(nodesCount, sizeof(float) * 8);
            }
        }

        private void stepRelaxAndCollision(int strandsCount, int strandNodesCount, int layerNodeStartIndex, Vector3 worldSpaceCameraPos,
            VerletSimulationSettings simulationSettings, float normalPercent) {
            compute.SetBuffer(verletKernel, "verletNodes", verletNodesBuffer);
            compute.SetBuffer(verletKernel, "verletNodesShape", verletNodesBuffer);
            compute.SetBuffer(verletKernel, "_RestPositions", verletRestPositionsBuffer);
            compute.SetFloat(ShaderID.SOURCE_MESH_NORMAL_TO_STRAND_NORMAL_PERCENT, normalPercent);
            compute.SetInt("verletNodesCount", nodesCount);
            compute.SetVector("worldSpaceCameraPos", worldSpaceCameraPos);
            compute.SetInt("layerVertexStartIndex", layerNodeStartIndex);
            compute.SetFloat("shapeConstraintRoot", simulationSettings.stiffnessRoot);
            compute.SetFloat("shapeConstraintTip", simulationSettings.stiffnessTip);
            compute.SetInt("numberOfFixedNodesInStrand", simulationSettings.isFirstNodeFixed ? 1 : 0);

            compute.SetVector("_Gravity", simulationSettings.gravity);
            compute.SetFloat("deltaTime", furRendererSettings.runPhysicsInFixedUpdate ? Time.fixedDeltaTime : Time.smoothDeltaTime);

            compute.SetFloat("_Decay", 1f - simulationSettings.drag);
            compute.SetFloat("stepSize", 1f / simulationSettings.constraintIterations);
            compute.SetInt("solverIterations", simulationSettings.constraintIterations);
            var nearestPow = CullAndSortController.nextPowerOf2(strandNodesCount);
            var dispatchCount = nearestPow * strandsCount;

            compute.SetInt("nearestPow", nearestPow);
            compute.SetInt("strandPointsCount", strandNodesCount);

            compute.setKeywordEnabled("COLLIDE_WITH_SOURCE_MESH", simulationSettings.isVerletColliderEnabled());
            compute.setKeywordEnabled("USE_FORWARD_COLLISION", simulationSettings.useForwardCollision);
            compute.Dispatch(verletKernel, dispatchCount.toCsGroups(), 1, 1);
        }

        public void dispose() {
            verletNodesBuffer?.Dispose();
            verletRestPositionsBuffer?.Dispose();
        }

        public void setupVerlet(ComputeShader furSetupComputeShader, int kernel, VerletSimulationSettings simulationSettings) {
            if (simulationSettings?.enableMovement == true && Application.isPlaying) {
                initBuffers();
                furSetupComputeShader.SetBuffer(kernel, "_RestPositions", verletRestPositionsBuffer);
                furSetupComputeShader.SetBuffer(kernel, "verletNodes", verletNodesBuffer);
                furSetupComputeShader.EnableKeyword("VERLET_ENABLED");
                furSetupComputeShader.setKeywordEnabled("INIT_VERLET", framesCount < 10);
                compute.setKeywordEnabled("INIT_VERLET", true);
                framesCount++;
            } else {
                furSetupComputeShader.DisableKeyword("VERLET_ENABLED");
                furSetupComputeShader.DisableKeyword("INIT_VERLET");
                compute.DisableKeyword("INIT_VERLET");
            }
        }

        private int framesCount;

        public void setupWind(FurRenderer fr) { setupWind(compute, verletKernel, fr.furRendererSettings); }

        public static void setupWind(ComputeShader computeShader, int kernel, FurRendererSettings furRendererSettings) {
            computeShader.SetFloat(ShaderID.WIND_GUST, furRendererSettings.windProperties.gustFrequency);
            computeShader.SetFloat(ShaderID.TIME, Time.time);
            computeShader.SetFloat(ShaderID.WIND_STRENGTH, furRendererSettings.windProperties.windStrength);
            var windForwardDirection = Quaternion.Euler(0f, furRendererSettings.windProperties.windDirectionDegree, 0f) * Vector3.forward;
            computeShader.SetVector(ShaderID.WIND_DIRECTION, windForwardDirection);
            computeShader.SetTexture(kernel, ShaderID.WIND_DISTORTION_MAP, furRendererSettings.windProperties.getWindTexture());
        }

        public void setFurMeshBuffer(GraphicsBuffer furMeshBuffer, int furMeshBufferStride) {
            compute.SetInt(ShaderID.FUR_MESH_BUFFER_STRIDE, furMeshBufferStride);
            compute.SetBuffer(verletKernel, ShaderID.FUR_MESH_BUFFER, furMeshBuffer);
        }

        public int getKernel() { return verletKernel; }
    }
}

[Serializable]
public class VerletSimulationSettings : ICloneable {
    public bool enableMovement = true;

    [Range(4, 32),
     Tooltip(
         "How many times the Verlet physics solver should loop. Higher value gives more precise physics with less stretching but comes at the cost of performance.")]
    public int constraintIterations = 18;

    [Tooltip("Gravity direction.")] public Vector3 gravity = Vector3.down * 0.3f;

    [Tooltip("Low value makes it very bouncy. High values will make it look like its under water.")] [Range(0f, 1f)]
    public float drag = 0.5f;

    [Tooltip("Hair shape constraint at the root of the strand.")] [Range(0f, 200f)]
    public float stiffnessRoot = 30f;

    [Tooltip("Hair shape constraint at the tip of the strand.")] [Range(0f, 200f)]
    public float stiffnessTip = 5f;

    [Tooltip(
        "This will create a realtime Signed Distance Field(SDF) collider of the source mesh, that the hairs will collide with. Comes with some performance cost.")]
    public bool collideWithSourceMesh;

    [Tooltip("How far out the hair particles will be pushed from the collider.")] [Range(0f, 0.1f)]
    public float colliderSkinWidth = 0.01f;

    [Tooltip("How far out the hair particles will be pushed from the collider.")] [Range(4f, 90f)]
    public int sdfColliderResolution = 15;

    [HideInInspector, Range(1f, 1.4f)] public float gridAllocationMultiplier = 1f;
    [HideInInspector, Range(0.7f, 1f)] public float extraPaddingInCells = 0.9f;

    [Tooltip("More precise collision, but can sometimes be a bit slower.")]
    public bool useForwardCollision = true;

    public bool isFirstNodeFixed;
    [Range(0.0f, 50f)] public float keepShapeStrength;

    public bool isUnsupportedSDFPlatform;

    public bool isVerletColliderEnabled() { return collideWithSourceMesh && enableMovement && !isUnsupportedSDFPlatform; }

    public bool isSDFCollisionEnabled() { return enableMovement && !isUnsupportedSDFPlatform; }

    public object Clone() {
        return new VerletSimulationSettings {
            enableMovement = enableMovement,
            constraintIterations = constraintIterations,
            gravity = gravity,
            drag = drag,
            stiffnessRoot = stiffnessRoot,
            stiffnessTip = stiffnessTip,
            collideWithSourceMesh = collideWithSourceMesh,
            colliderSkinWidth = colliderSkinWidth,
            sdfColliderResolution = sdfColliderResolution,
            gridAllocationMultiplier = gridAllocationMultiplier,
            useForwardCollision = useForwardCollision,
            isFirstNodeFixed = isFirstNodeFixed,
            isUnsupportedSDFPlatform = isUnsupportedSDFPlatform
        };
    }
}