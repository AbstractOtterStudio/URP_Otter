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
    public int BlurStrength = 1;
    Material material = null;
    int tmpTexId = Shader.PropertyToID("_ShorelineFilterTempBuffer");
    RenderTargetIdentifier ShorelineTex;
    RenderTargetIdentifier TmpTex;

    public ShorelineFilterPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
        //this.material = CoreUtils.CreateEngineMaterial("Hidden/Box Blur");
        this.material = CoreUtils.CreateEngineMaterial("Unlit/BoxBlur");
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
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
            Shader.SetGlobalFloat(Shader.PropertyToID("_BoxBlurStrength"), BlurStrength);
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
    public int BlurStrenth = 1;

    ShorelineFilterPass renderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        renderPass.BlurStrength = BlurStrenth;
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