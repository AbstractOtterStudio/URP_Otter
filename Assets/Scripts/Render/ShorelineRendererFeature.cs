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
    //public Vector3 ExpansionStart = new Vector3(0.3f, 0, 0.3f);
    //public Vector3 ExpansionEnd = new Vector3(0.5f, 0, 0.5f);
    public float MaxDepth = 0.2f;
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
        descriptor.width = Mathf.CeilToInt(descriptor.width / WaterRenderProperties.ShorelineDownsampling.x);
        descriptor.height = Mathf.CeilToInt(descriptor.height / WaterRenderProperties.ShorelineDownsampling.y);
        // descriptor.colorFormat = RenderTextureFormat.R8;
        descriptor.colorFormat = RenderTextureFormat.RFloat;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

        cmd.GetTemporaryRT(WaterRenderProperties.ShorelineBufferID, descriptor);
        ShorelineTex = new RenderTargetIdentifier(WaterRenderProperties.ShorelineBufferID);

        //ConfigureTarget(ShorelineTex, 
        //    new RenderTargetIdentifier(WaterRenderProperties.WaterDepthBufferID, 0, CubemapFace.Unknown, -1));

        ConfigureTarget(ShorelineTex, ShorelineTex);

        ConfigureClear(ClearFlag.Color, Color.clear);
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
            //cmd.SetGlobalVector(WaterRenderProperties.ShorelineExpansionStartID, ExpansionStart);
            //cmd.SetGlobalVector(WaterRenderProperties.ShorelineExpansionEndID, ExpansionEnd);
            //cmd.EnableShaderKeyword("__WRITE_SHORELINE_BUFFER");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        //using (new ProfilingScope(cmd, new ProfilingSampler("Clear Shoreline Mask At Beginning")))
        //{
        //    cmd.DisableShaderKeyword("__WRITE_SHORELINE_BUFFER");
        //    context.ExecuteCommandBuffer(cmd);
        //    cmd.Clear();
        //    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        //    cmd.SetGlobalTexture(WaterRenderProperties.ShorelineBufferID, ShorelineTex);
        //    cmd.SetGlobalFloat(WaterRenderProperties.ShorelineMaxDepthID, MaxDepth);
        //}
        //context.ExecuteCommandBuffer(cmd);
        //CommandBufferPool.Release(cmd);
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
    //public Vector3 ExpansionStart = new Vector3(0.3f, 0, 0.3f);
    //public Vector3 ExpansionEnd = new Vector3(0.5f, 0, 0.5f);
    public float MaxDepth = 0.2f;

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
    //    renderPass.MaxDepth = MaxDepth;
    //}

    public override void Create()
    {
        renderPass = new ShorelinePass(InjectionPoint, Layer);
        //renderPass.ExpansionStart = ExpansionStart;
        //renderPass.ExpansionEnd = ExpansionEnd;
        renderPass.MaxDepth = MaxDepth;
    }
    protected override void Dispose(bool disposing)
    {
        renderPass.Dispose();
    }
}