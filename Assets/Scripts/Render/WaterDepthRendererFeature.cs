using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>
/// Render all meshes of a certain layer mask to a custom depth buffer
/// </summary>
public class WaterDepthPass : ScriptableRenderPass
{
    LayerMask layerMask;
    FilteringSettings filteringSettings;
    List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    RenderTargetIdentifier WaterDepthTex;

    public WaterDepthPass(RenderPassEvent renderPassEvent, LayerMask layerMask)
    {
        this.renderPassEvent = renderPassEvent;
        this.layerMask = layerMask;
        filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        shaderTagIdList.Add(new ShaderTagId("DepthOnly")); // Only render DepthOnly pass
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        // descriptor.colorFormat = RenderTextureFormat.RFloat;
        // descriptor.depthBufferBits = 0;
        descriptor.colorFormat = RenderTextureFormat.Depth;
        descriptor.msaaSamples = 1;

        cmd.GetTemporaryRT(WaterRenderProperties.WaterDepthBufferID, descriptor, FilterMode.Point);
        WaterDepthTex = new RenderTargetIdentifier(WaterRenderProperties.WaterDepthBufferID, 0, CubemapFace.Unknown, -1);

        ConfigureTarget(WaterDepthTex, WaterDepthTex);
        //ConfigureClear(ClearFlag.All, Color.black);
        ConfigureClear(ClearFlag.All, Color.white); // doesn't work
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(WaterRenderProperties.WaterDepthBufferID);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = SortingCriteria.None;
        DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
        drawingSettings.perObjectData = PerObjectData.None;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Draw Water Depth")))
        {
            //cmd.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            //cmd.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
            //context.ExecuteCommandBuffer(cmd);
            //cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            cmd.SetGlobalTexture(WaterRenderProperties.WaterDepthBufferID, WaterDepthTex);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        //waterDepthTex?.Release();
    }
}

public class WaterDepthRendererFeature : ScriptableRendererFeature
{
    public LayerMask Layer;
    public RenderPassEvent InjectionPoint = RenderPassEvent.AfterRenderingShadows;

    WaterDepthPass renderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(renderPass);
    }

    public override void Create()
    {
        renderPass = new WaterDepthPass(InjectionPoint, Layer);
    }
    protected override void Dispose(bool disposing)
    {
        renderPass.Dispose();
    }
}