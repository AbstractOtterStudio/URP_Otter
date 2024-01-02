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
    public FilterSize filterSize = FilterSize.Five;
    Material material = null;
    int tmpTexId = Shader.PropertyToID("_ShorelineFilterTempBuffer");
    RenderTargetIdentifier ShorelineTex;
    RenderTargetIdentifier TmpTex;

    public ShorelineFilterPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
        //this.material = CoreUtils.CreateEngineMaterial("Hidden/Box Blur");
        this.material = CoreUtils.CreateEngineMaterial("Water/ShorelineBlur");
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.width = Mathf.CeilToInt(descriptor.width / WaterRenderProperties.ShorelineDownsampling.x);
        descriptor.height = Mathf.CeilToInt(descriptor.height / WaterRenderProperties.ShorelineDownsampling.y);
        // descriptor.colorFormat = RenderTextureFormat.R8;
        descriptor.colorFormat = RenderTextureFormat.RFloat;
        descriptor.depthBufferBits = 0;
        //descriptor.msaaSamples = 1;

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
        using (new ProfilingScope(cmd, new ProfilingSampler("Filt Shoreline Mask")))
        {
            switch (filterSize)
            {
                default:
                case FilterSize.Three:
                    cmd.EnableShaderKeyword("__BLUR_SIZE_3");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_5");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_7");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_9");
                    break;
                case FilterSize.Five:
                    cmd.EnableShaderKeyword("__BLUR_SIZE_5");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_3");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_7");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_9");
                    break;
                case FilterSize.Seven:
                    cmd.EnableShaderKeyword("__BLUR_SIZE_7");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_3");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_5");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_9");
                    break;
                case FilterSize.Nine:
                    cmd.EnableShaderKeyword("__BLUR_SIZE_9");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_3");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_5");
                    cmd.DisableShaderKeyword("__BLUR_SIZE_7");
                    break;
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            Blit(cmd, ShorelineTex, TmpTex, material, 0); // shader pass 0
            Blit(cmd, TmpTex, ShorelineTex, material, 1); // shader pass 1
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

    ShorelineFilterPass renderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        renderPass.filterSize = filterSize;
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(renderPass);
    }

    public override void Create()
    {
        renderPass = new ShorelineFilterPass(InjectionPoint);
    }
    protected override void Dispose(bool disposing)
    {
        renderPass.Dispose();
    }
}