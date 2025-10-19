#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.Rendering;
using UnityEngine.Rendering;

namespace Pinwheel.Griffin
{
    //[CreateAssetMenu(menuName = "Griffin/Runtime Settings")]
    [ExecuteInEditMode]
    public partial class GRuntimeSettings : ScriptableObject
    {
        private static GRuntimeSettings instance;
        public static GRuntimeSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GRuntimeSettings>("PolarisRuntimeSettings");
                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<GRuntimeSettings>();
                    }
                }
                return instance;
            }
        }

        public GeometryDefaultSettings geometryDefault = new GeometryDefaultSettings();
        public ShadingDefaultSettings shadingDefault = new ShadingDefaultSettings();
        public RenderingDefaultSettings renderingDefault = new RenderingDefaultSettings();
        public FoliageDefaultSettings foliageDefault = new FoliageDefaultSettings();
        public MaskDefaultSettings maskDefault = new MaskDefaultSettings();
        public GeometryGenerationSettings geometryGeneration = new GeometryGenerationSettings();
        public TerrainRenderingSettings terrainRendering = new TerrainRenderingSettings();
        public FoliageRenderingSettings foliageRendering = new FoliageRenderingSettings();
        public InternalShaderSettings internalShaders = new InternalShaderSettings();
        public DefaultTexturesSettings defaultTextures = new DefaultTexturesSettings();

        public bool isEditingGeometry;
        public bool isEditingFoliage;

        public void OnEnable()
        {
            //GGrassMeshGenerator.Generate();
        }

        public void OnDisable()
        {
            GGrassMeshGenerator.CleanUp();
        }
    }

    public partial class GRuntimeSettings : ScriptableObject
    {
        [System.Serializable]
        public class GeometryDefaultSettings
        {
            public float width;
            public float height;
            public float length;
            public int heightMapResolution;
            public int meshBaseResolution;
            public int meshResolution;
            public int chunkGridSize;
            public int lodCount;
            public int displacementSeed;
            public float displacementStrength;
            public GAlbedoToVertexColorMode albedoToVertexColorMode;
            public GGeometry.GStorageMode storageMode;
            public bool allowTimeSlicedGeneration;
            public bool smoothNormal;
            public bool useSmoothNormalMask;
            public bool mergeUv;
        }

        [System.Serializable]
        public class ShadingDefaultSettings
        {
            public int albedoMapResolution;
            public int metallicMapResolution;
            public string albedoMapPropertyName;
            public string metallicMapPropertyName;
            public Gradient colorByHeight;
            public Gradient colorByNormal;
            public AnimationCurve colorBlendCurve;
            public string colorByHeightPropertyName;
            public string colorByNormalPropertyName;
            public string colorBlendPropertyName;
            public string dimensionPropertyName;
            public int splatControlResolution;
            public string splatControlMapPropertyName;
            public string splatMapPropertyName;
            public string splatNormalPropertyName;
            public string splatMetallicPropertyName;
            public string splatSmoothnessPropertyName;
            //public GSplatPrototypeGroup splats;
        }

        [System.Serializable]
        public class RenderingDefaultSettings
        {
            public bool terrainCastShadow;
            public bool terrainReceiveShadow;

            public bool drawTrees;
            [System.Obsolete]
            public bool enableInstancing;
            public float treeLod1Start;
            public float billboardStart;
            public float treeDistance;
            public float treeLodTransitionDistance;
            public float treeCullBias;
            public GTreeCullMode treeCullMode = GTreeCullMode.Precise;
            public int treeCullQuadTreeDepth = 6;
            public LightProbeUsage treeLightProbeUsage;

            public bool drawGrasses;
            public float grassDistance;
            public float grassLod1Start;
            public float grassLodTransitionDistance;
            public int grassCellToProcessPerFrame;
            public float grassFadeStart;
            public float grassCullBias;
            public LightProbeUsage grassLightProbeUsage;
            public bool grassUnloadInactiveCells;
            public int grassFrameCountToUnloadInactiveCells;

            public bool drawFoliageOnMainCameraOnly;
        }

        [System.Serializable]
        public class FoliageDefaultSettings
        {
            public GSnapMode treeSnapMode;
            public LayerMask treeSnapLayerMask;
            public GSnapMode grassSnapMode;
            public LayerMask grassSnapLayerMask;
            public int patchGridSize;
            public bool enableInteractiveGrass;
            public int vectorFieldMapResolution;
            public float bendSensitive;
            public float restoreSensitive;
        }

        [System.Serializable]
        public class MaskDefaultSettings
        {
            public int maskMapResolution;
        }

        [System.Serializable]
        public class GeometryGenerationSettings
        {
            public int triangulateIteration;
            public bool useLodTransitionCurve;
            public AnimationCurve lodTransition = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }

        [System.Serializable]
        public class TerrainRenderingSettings
        {
            [System.Serializable]
            public class TerrainMaterialTemplate
            {
                public GLightingModel lightingModel;
                public GTexturingModel texturingModel;
                public GSplatsModel splatsModel;
                public Material material;
            }

            public List<TerrainMaterialTemplate> builtinRpMaterials;
            public List<TerrainMaterialTemplate> universalRpMaterials;

            public bool FindMaterialTemplate(Shader shader, GRenderPipelineType pipeline, out TerrainMaterialTemplate template)
            {
                template = default;
                List<TerrainMaterialTemplate> templateList =
                    pipeline == GRenderPipelineType.Builtin ? builtinRpMaterials :
                    pipeline == GRenderPipelineType.Universal ? universalRpMaterials :
                    null;
                if (templateList == null)
                {
                    return false;
                }

                int index = templateList.FindIndex((t) =>
                {
                    return t.material != null && t.material.shader == shader;
                });

                if (index >= 0)
                {
                    template = templateList[index];
                }

                return index >= 0;
            }

            public Material GetClonedMaterial(GRenderPipelineType pipeline, GLightingModel light, GTexturingModel texturing, GSplatsModel splats = GSplatsModel.Splats4)
            {
                TerrainMaterialTemplate matTemplate;
                List<TerrainMaterialTemplate> collection =
                    pipeline == GRenderPipelineType.Universal ? universalRpMaterials :
                    builtinRpMaterials;

                if (texturing != GTexturingModel.Splat)
                {
                    matTemplate = collection.Find(m =>
                    {
                        return m.lightingModel == light && m.texturingModel == texturing;
                    });
                }
                else
                {
                    matTemplate = collection.Find(m =>
                    {
                        return m.lightingModel == light && m.texturingModel == texturing && m.splatsModel == splats;
                    });
                }

                Material mat = null;
                if (matTemplate.material != null)
                {
                    mat = UnityEngine.Object.Instantiate(matTemplate.material);
                }

                return mat;
            }

            public Shader GetTerrainShader(GRenderPipelineType pipeline, GLightingModel light, GTexturingModel texturing, GSplatsModel splats = GSplatsModel.Splats4)
            {
                TerrainMaterialTemplate matTemplate;
                List<TerrainMaterialTemplate> collection =
                    pipeline == GRenderPipelineType.Universal ? universalRpMaterials :
                    builtinRpMaterials;

                if (texturing != GTexturingModel.Splat)
                {
                    matTemplate = collection.Find(m =>
                    {
                        return m.lightingModel == light && m.texturingModel == texturing;
                    });
                }
                else
                {
                    matTemplate = collection.Find(m =>
                    {
                        return m.lightingModel == light && m.texturingModel == texturing && m.splatsModel == splats;
                    });
                }

                if (matTemplate.material != null)
                {
                    return matTemplate.material.shader;
                }
                else
                {
                    return null;
                }
            }
        }

        [System.Serializable]
        public class FoliageRenderingSettings
        {
            public Mesh grassQuad;
            public Mesh grassCross;
            public Mesh grassTriCross;
            public Mesh grassClump;

            public Mesh grassQuadLod1;
            public Mesh grassCrossLod1;
            public Mesh grassTriCrossLod1;
            public Mesh grassClumpLod1;

            public Material treeBillboardMaterial;
            public Material grassMaterial;
            public Material grassBillboardMaterial;
            public Material grassInteractiveMaterial;
            public Material grassLod1Material;

            public Material urpTreeBillboardMaterial;
            public Material urpGrassMaterial;
            public Material urpGrassBillboardMaterial;
            public Material urpGrassInteractiveMaterial;
            public Material urpGrassLod1Material;

            public Texture2D windNoiseTexture;

            public Mesh GetGrassMesh(GGrassShape shape)
            {
                if (shape == GGrassShape.Quad)
                {
                    if (grassQuad == null)
                        grassQuad = GGrassMeshGenerator.GenerateQuad();
                    return grassQuad;
                }
                else if (shape == GGrassShape.Cross)
                {
                    if (grassCross == null)
                        grassCross = GGrassMeshGenerator.GenerateCross();
                    return grassCross;
                }
                else if (shape == GGrassShape.TriCross)
                {
                    if (grassTriCross == null)
                        grassTriCross = GGrassMeshGenerator.GenerateTriCross();
                    return grassTriCross;
                }
                else if (shape == GGrassShape.Clump)
                {
                    if (grassClump == null)
                        grassClump = GGrassMeshGenerator.GenerateClump(GetGrassMesh(GGrassShape.Quad));
                    return grassClump;
                }
                else
                    return null;
            }

            public Mesh GetGrassMeshLod1(GGrassShape shape)
            {
                if (shape == GGrassShape.Quad)
                {
                    if (grassQuadLod1 == null)
                        grassQuadLod1 = GGrassMeshGenerator.GenerateQuadLod1();
                    return grassQuadLod1;
                }
                else if (shape == GGrassShape.Cross)
                {
                    if (grassCrossLod1 == null)
                        grassCrossLod1 = GGrassMeshGenerator.GenerateCrossLod1();
                    return grassCrossLod1;
                }
                else if (shape == GGrassShape.TriCross)
                {
                    if (grassTriCrossLod1 == null)
                        grassTriCrossLod1 = GGrassMeshGenerator.GenerateTriCrossLod1();
                    return grassTriCrossLod1;
                }
                else if (shape == GGrassShape.Clump)
                {
                    if (grassClumpLod1 == null)
                        grassClumpLod1 = GGrassMeshGenerator.GenerateClump(GetGrassMeshLod1(GGrassShape.Quad));
                    return grassClumpLod1;
                }
                else
                    return null;
            }
        }

        [System.Serializable]
        public class InternalShaderSettings
        {
            public Shader blitShader;
            public Shader solidColorShader;
            public Shader copyTextureShader;
            public Shader subDivisionMapShader;
            public Shader blurShader;
            public Shader blurRadiusShader;
            public Shader elevationPainterShader;
            public Shader heightSamplingPainterShader;
            public Shader subdivPainterShader;
            public Shader painterCursorProjectorShader;
            public Shader albedoPainterShader;
            public Shader metallicPainterShader;
            public Shader smoothnessPainterShader;
            public Shader splatPainterShader;
            public Shader visibilityPainterShader;
            public Shader rampMakerShader;
            public Shader pathPainterAlbedoShader;
            public Shader pathPainterMetallicSmoothnessShader;
            public Shader pathPainterSplatShader;
            public Shader geometryLivePreviewShader;
            public Shader geometricalHeightMapShader;
            public Shader splineMaskShader;
            public Shader maskVisualizerShader;
            public Shader stamperShader;
            public Shader terrainNormalMapShader;
            public Shader terrainPerPixelNormalMapShader;
            public Shader textureStamperBrushShader;
            public Shader grassPreviewShader;
            public Shader navHelperDummyGameObjectShader;
            public Shader splatsToAlbedoShader;
            public Shader unlitChannelMaskShader;
            public Shader channelToGrayscaleShader;
            public Shader heightMapFromMeshShader;
            public Shader curveFilterShader;
            public Shader invertFilterShader;
            public Shader stepFilterShader;
            public Shader warpFilterShader;
            public Shader steepnessMapGeneratorShader;
            public Shader noiseMapGeneratorShader;
            public Shader blendMapGeneratorShader;
            public Shader distributionMapGeneratorShader;
            public Shader interactiveGrassVectorFieldShader;
            public Shader subdivLivePreviewShader;
            public Shader visibilityLivePreviewShader;
            public Shader terracePainterShader;
            public Shader remapPainterShader;
            public Shader noisePainterShader;
            public Shader heightmapConverterEncodeRGShader;
            public Shader heightmapDecodeGrayscaleShader;
            public Shader drawTex2DArraySliceShader;
            public Shader maskPainterShader;
            public Shader mask4ChannelsShader;
            public Shader fetchWorldDataShader;
            public Shader applyErosionShader;
            public Shader erosionTexturerShader;
            public Shader vegetationDitheringMaskShader;

            public ComputeShader hydraulicErosionShader;
            public ComputeShader thermalErosionShader;
        }

        [System.Serializable]
        public class DefaultTexturesSettings
        {
            public Texture2D redTexture;
            public Texture2D blackTexture;
            public Texture2D clearTexture;
        }
    }
}
#endif
