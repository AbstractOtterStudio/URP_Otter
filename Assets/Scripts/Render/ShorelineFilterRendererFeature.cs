using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

/// <summary>
/// Render all meshes of a certain layer mask to a custom depth buffer
/// </summary>
public class ShorelineFilterPass : ScriptableRenderPass
{
    public enum FilterSize { Three, Five, Seven, Nine };
    public int dilation1 = 2;
    public int dilation2 = 3;
    Material material = null;
    int tmpTexId = Shader.PropertyToID("_ShorelineFilterTempBuffer");
    RenderTargetIdentifier ShorelineTex;
    RenderTargetIdentifier TmpTex;

    public ShorelineFilterPass(RenderPassEvent renderPassEvent, FilterSize filterSize)
    {
        this.renderPassEvent = renderPassEvent;
        //this.material = CoreUtils.CreateEngineMaterial("Hidden/Box Blur");
        this.material = CoreUtils.CreateEngineMaterial("Water/ShorelineBlur");

        switch (filterSize)
        {
            default:
            case FilterSize.Three:
                Shader.EnableKeyword("__BLUR_SIZE_3");
                Shader.DisableKeyword("__BLUR_SIZE_5");
                Shader.DisableKeyword("__BLUR_SIZE_7");
                Shader.DisableKeyword("__BLUR_SIZE_9");
                break;
            case FilterSize.Five:
                Shader.EnableKeyword("__BLUR_SIZE_5");
                Shader.DisableKeyword("__BLUR_SIZE_3");
                Shader.DisableKeyword("__BLUR_SIZE_7");
                Shader.DisableKeyword("__BLUR_SIZE_9");
                break;
            case FilterSize.Seven:
                Shader.EnableKeyword("__BLUR_SIZE_7");
                Shader.DisableKeyword("__BLUR_SIZE_3");
                Shader.DisableKeyword("__BLUR_SIZE_5");
                Shader.DisableKeyword("__BLUR_SIZE_9");
                break;
            case FilterSize.Nine:
                Shader.EnableKeyword("__BLUR_SIZE_9");
                Shader.DisableKeyword("__BLUR_SIZE_3");
                Shader.DisableKeyword("__BLUR_SIZE_5");
                Shader.DisableKeyword("__BLUR_SIZE_7");
                break;
        }
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

        cmd.GetTemporaryRT(tmpTexId, descriptor);
        TmpTex = new RenderTargetIdentifier(tmpTexId);
        ShorelineTex = new RenderTargetIdentifier(WaterRenderProperties.ShorelineBufferID);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(WaterRenderProperties.ShorelineBufferID);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Filter Shoreline Mask")))
        {
            cmd.SetGlobalFloat(Shader.PropertyToID("_filter_dilation"), dilation1);
            Blit(cmd, ShorelineTex, TmpTex, material, 2);
            Blit(cmd, TmpTex, ShorelineTex, material, 3);
            Blit(cmd, ShorelineTex, TmpTex, material, 2);
            Blit(cmd, TmpTex, ShorelineTex, material, 3);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.SetGlobalFloat(Shader.PropertyToID("_filter_dilation"), dilation2);
            Blit(cmd, ShorelineTex, TmpTex, material, 0);
            Blit(cmd, TmpTex, ShorelineTex, material, 1);
            Blit(cmd, ShorelineTex, TmpTex, material, 0);
            Blit(cmd, TmpTex, ShorelineTex, material, 1);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        material = null;
    }
}

public class ShorelineFilterRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingOpaques;
    public ShorelineFilterPass.FilterSize filterSize = ShorelineFilterPass.FilterSize.Five;
    public int dilation1 = 2;
    public int dilation2 = 3;

    ShorelineFilterPass renderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(renderPass);
    }

    public override void Create()
    {
        renderPass = new ShorelineFilterPass(InjectionPoint, filterSize);
        renderPass.dilation1 = dilation1;
        renderPass.dilation2 = dilation2;
    }
    protected override void Dispose(bool disposing)
    {
        renderPass.Dispose();
    }
}