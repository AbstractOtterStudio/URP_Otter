using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [Serializable]
    public class HairStrandLayer : ICloneable {
        [SerializeField, HideInInspector] public HairStrand[] layerHairStrands = new HairStrand[0];
        [SerializeField, HideInInspector] public ClumpModifierLayer[] clumpsModifiers = new ClumpModifierLayer[0];
        [SerializeField, HideInInspector] public CardMeshProperties cardMeshProperties = new CardMeshProperties();
        [SerializeField, HideInInspector] public VerletSimulationSettings verletSimulationSettings;
        [SerializeField, HideInInspector] public float sourceMeshNormalToStrandNormalPercent = -1;
        [SerializeField, HideInInspector] public string layerName;
        private Mesh _cardMesh;
        public ComputeBuffer hairStrandsBuffer;

        public ComputeShader meshBakerComputeShader;
        private MeshBaker meshBaker;

        public Mesh CardMesh {
            get {
                if (_cardMesh == null || meshBaker == null) {
                    recreateCardMesh();
                }

                return _cardMesh;
            }
        }

        public void recreateCardMesh() {
            meshBaker?.dispose();
            _cardMesh = FurMeshCreator.CreateHairCard(cardMeshProperties);
            if (meshBakerComputeShader == null)
                meshBakerComputeShader = Resources.Load<ComputeShader>(ShaderID.MESH_BAKER_FULL_CS_NAME);
            meshBaker = new MeshBaker(_cardMesh, meshBakerComputeShader, sizeof(float) * 16);
        }

        public bool hasClumps() {
            return clumpsModifiers.Length > 0;
        }

        public ComputeBuffer getLastClumpModifierBuffer() {
            return getLastClumpModifier().clumpsPositionBuffer;
        }

        public ComputeBuffer getLastClumpAttractionBuffer() {
            return getLastClumpModifier().clumpAttractionBuffer;
        }

        public ClumpModifierLayer getLastClumpModifier() {
            return clumpsModifiers[clumpsModifiers.Length - 1];
        }

        public void recreateHairStrandsBuffers() {
            hairStrandsBuffer?.Dispose();
            hairStrandsBuffer = new ComputeBuffer(layerHairStrands.Length, HairStrandStruct.size());

            var instancedHairCardsStruct = layerHairStrands.ToList().Select(it => it.convertToStruct()).ToArray();

            hairStrandsBuffer.SetData(instancedHairCardsStruct);
        }

        public void copyValuesFromComputeBufferToNativeObject() {
            if (hairStrandsBuffer != null) AsyncGPUReadback.Request(hairStrandsBuffer, onCompleteReadback);
        }


        private void onCompleteReadback(AsyncGPUReadbackRequest request) {
            if (request.hasError) {
                Debug.Log("GPU readback error detected for hairStrandsBuffer.");
                return;
            }

            var hairStrandStructs = request.ToPersistentArray<HairStrandStruct>();
            new Thread(() => {
                layerHairStrands = hairStrandStructs.Select(it => it.convertToObject()).ToArray();
                hairStrandStructs.Dispose();
            }).Start();
        }

        public void dispose() {
            hairStrandsBuffer?.Dispose();
            hairStrandsBuffer = null;
            meshBaker?.dispose();

            foreach (var clumpsModifier in clumpsModifiers) {
                clumpsModifier.dispose();
            }
        }

        public void passCardMeshPropertiesToComputeShader(int kernel, ComputeShader computeShader) {
            if (hairStrandsBuffer == null) {
                //May have been disposed by another objects onDisable
                recreateHairStrandsBuffers();
                recreateCardMesh();
            }

            computeShader.SetBuffer(kernel, "hairStrandsBuffer", hairStrandsBuffer);
            computeShader.SetBuffer(kernel, "cardMesh", meshBaker.bakedMesh);
            computeShader.SetInt("cardMeshVerticesCount", CardMesh.vertexCount);
        }

        public HairStrandLayer setupCardShape(GroomContainer groomContainer) {
            if (groomContainer.isUsingCardPreset) {
                cardMeshProperties.shapeCurve = AnimationCurve.Linear(0, 1, 1, 1);
                cardMeshProperties.cardSubdivisionsY = 3;
            }

            return this;
        }

        public object Clone() {
            return new HairStrandLayer() {
                layerHairStrands = layerHairStrands,
                clumpsModifiers = clumpsModifiers.Select(e => (ClumpModifierLayer)e.Clone()).ToArray(),
                cardMeshProperties = (CardMeshProperties)cardMeshProperties.Clone()
            };
        }
    }
}