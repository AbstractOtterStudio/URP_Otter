using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>
/// Render all meshes of a certain layer mask to a custom depth buffer
/// </summary>
public class ShorelinePass : ScriptableRenderPass
{
    public Vector3 Expansion = new Vector3(0.5f, 0, 0.5f);
    public float MinimalDepth = 0.2f;
    LayerMask layerMask;
    FilteringSettings filteringSettings;
    List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    RenderTargetIdentifier ShorelineTex;

    public ShorelinePass(RenderPassEvent renderPassEvent, LayerMask layerMask)
    {
        this.renderPassEvent = renderPassEvent;
        this.layerMask = layerMask;
        filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
        shaderTagIdList.Add(new ShaderTagId("WorldSpaceOutline"));
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        // descriptor.colorFormat = RenderTextureFormat.RFloat;
        // descriptor.depthBufferBits = 0;
        descriptor.colorFormat = RenderTextureFormat.RHalf;
        //descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

        cmd.GetTemporaryRT(WaterRenderProperties.ShorelineBufferID, descriptor);
        ShorelineTex = new RenderTargetIdentifier(WaterRenderProperties.ShorelineBufferID);

        ConfigureTarget(ShorelineTex/*, renderingData.cameraData.renderer.cameraDepthTarget*/);
        ConfigureClear(ClearFlag.Color, Color.clear);
        ConfigureClear(ClearFlag.Depth, Color.white);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(WaterRenderProperties.ShorelineBufferID);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = SortingCriteria.None;
        DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
        drawingSettings.perObjectData = PerObjectData.None;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Draw Shoreline Mask")))
        {
            cmd.SetGlobalFloat(WaterRenderProperties.ShorelineMinimalDepthID, MinimalDepth);
            cmd.SetGlobalVector(WaterRenderProperties.ShorelineExpansionID, Expansion);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            cmd.SetGlobalTexture(WaterRenderProperties.ShorelineBufferID, ShorelineTex);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        //ShorelineTex?.Release();
    }
}

public class ShorelineRendererFeature : ScriptableRendererFeature
{
    public LayerMask Layer;
    public RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingPrepasses;
    public Vector3 Expansion = new Vector3(0.5f, 0, 0.5f);
    public float MinimalDepth = 0.2f;

    ShorelinePass renderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(renderPass);
    }

    // Doesn't have in this version
    //public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    //{
    //    renderPass.Expansion = Expansion;
    //    renderPass.MinimalDepth = MinimalDepth;
    //}

    public override void Create()
    {
        renderPass = new ShorelinePass(InjectionPoint, Layer);
        renderPass.Expansion = Expansion;
        renderPass.MinimalDepth = MinimalDepth;
    }
    protected override void Dispose(bool disposing)
    {
        renderPass.Dispose();
    }
}