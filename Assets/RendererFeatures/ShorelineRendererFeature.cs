using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class ShorelinePass : ScriptableRenderPass
{
    public Material material;
    RenderTargetIdentifier shorelineBuffer;
    static string ProfilerTag = "Draw Shoreline Buffer";
    int ShorelineBufferID = Shader.PropertyToID(WaterRenderProperties.ShorelineBufferName);
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Grab the camera target descriptor. We will use this when creating a temporary render texture.
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

        // Set the number of depth bits we need for our temporary render texture.
        descriptor.depthBufferBits = 16;
        descriptor.colorFormat = RenderTextureFormat.R8;

        // Create a temporary render texture using the descriptor from above.
        cmd.GetTemporaryRT(ShorelineBufferID, descriptor, FilterMode.Bilinear);
        shorelineBuffer = new RenderTargetIdentifier(ShorelineBufferID);
    }
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(ShorelineBufferID);
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //CommandBuffer cmd = CommandBufferPool.Get();
        //using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        //{

        //}

        //// Execute the command buffer and release it.
        //context.ExecuteCommandBuffer(cmd);
        //CommandBufferPool.Release(cmd);
    }
}