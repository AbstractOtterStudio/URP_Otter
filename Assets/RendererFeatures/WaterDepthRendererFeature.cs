using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal.Internal;
using static UnityEngine.XR.XRDisplaySubsystem;
using static UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure;
using UnityEngine.Experimental.Rendering;
using Microsoft.SqlServer.Server;

public class WaterDepthPass : ScriptableRenderPass
{
    LayerMask layerMask;
    RenderTargetIdentifier waterDepthBuffer;
    static string ProfilerTag = "Draw Water Depth Buffer";
    int WaterDepthBufferID = Shader.PropertyToID(WaterRenderProperties.WaterDepthBufferName);

    ProfilingSampler profilingSampler;
    FilteringSettings filteringSettings;
    List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
    RenderTargetIdentifier waterDepthTex;

    public WaterDepthPass(RenderPassEvent renderPassEvent, LayerMask layerMask)
    {
        this.renderPassEvent = renderPassEvent;
        this.layerMask = layerMask;
        profilingSampler = new ProfilingSampler(ProfilerTag);
        filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        shaderTagIdList.Add(new ShaderTagId("DepthOnly")); // Only render DepthOnly pass
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.colorFormat = RenderTextureFormat.Depth;
        //descriptor.depthBufferBits = 0;

        cmd.GetTemporaryRT(WaterDepthBufferID, descriptor, FilterMode.Point);
        waterDepthTex = new RenderTargetIdentifier(WaterDepthBufferID, 0, CubemapFace.Unknown, -1);
        ConfigureTarget(waterDepthTex);
        ConfigureClear(ClearFlag.All, Color.black);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(WaterDepthBufferID);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = SortingCriteria.None;
        DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
        drawingSettings.perObjectData = PerObjectData.None;
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, profilingSampler))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            //cmd.Blit(renderingData.cameraData.renderer.cameraDepthTarget, waterDepthTex);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

public class WaterDepthRendererFeature : ScriptableRendererFeature
{
    //public Shader Shader;
    public LayerMask Layer;
    public RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingPrepasses;

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
}