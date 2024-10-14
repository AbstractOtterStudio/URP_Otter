using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public enum OutlineTarget
{
    TakeAndEat,
    SeaGrass
}

public class OutLineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public RenderTexture rt;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
        public Material purMat;
        public Material edgeMat;
        public OutlineTarget renderListTarget;

    }

    public Setting setting;
    public OutLineRenderPass outLineRenderPass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var source = renderer.cameraColorTarget;
        var dest = RenderTargetHandle.CameraTarget;
        outLineRenderPass.SetUp(source, dest);
        renderer.EnqueuePass(outLineRenderPass);
    }

    public override void Create()
    {
        setting.rt = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 32);
        outLineRenderPass = new
            OutLineRenderPass(setting.rt, setting.renderPassEvent, setting.purMat, setting.edgeMat, setting.renderListTarget);
    }
}

public class OutLineRenderPass : ScriptableRenderPass
{
    public RenderTexture rt;
    public Material purMat;
    public Material edgeMat;
    RenderTargetIdentifier source;
    RenderTargetHandle dest;

    RenderTargetHandle temRT;

    CommandBuffer cmd;
    Renderer render;
    OutlineTarget target;

    public OutLineRenderPass(RenderTexture rt, RenderPassEvent rpv, Material purMat, Material edgeMat, OutlineTarget target)
    {
        renderPassEvent = rpv;
        this.rt = rt;
        this.purMat = purMat;
        this.edgeMat = edgeMat;
        this.target = target;
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //render = OutLineManager.renderer; 
        //if(render == null)
        //{
        //    return;
        //}
        List<Renderer> renderList = new List<Renderer>();
        if (PostProcessingManager.instance != null)
        {
            return;
            // renderList = PostProcessingManager.instance.playerController.GetTargetItemRendererList(target);
            //renderList = PostProcessingManager.instance.playerController.GetPlayerCanTakeItemRendererList();
        }

        if (renderList == null || renderList.Count <= 0) { return; }

        cmd = CommandBufferPool.Get("edgeCmd");
        cmd.SetRenderTarget(rt);

        //cmd.DrawRenderer(render,purMat,0,0);               

        if (renderList.Count > 0)
        {
            for (int i = 0; i < renderList.Count; i++)
            {
                if (renderList[i] == null) { continue; }
                cmd.DrawRenderer(renderList[i], purMat, 0, 0);
            }
        }
        else
        {
            throw new System.Exception($"renderList has problem");
        }

        edgeMat.SetTexture("_Mask", rt);

        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        cmd.GetTemporaryRT(temRT.id, desc);
        cmd.Blit(source, temRT.Identifier(), edgeMat, 0, 0);
        cmd.Blit(temRT.Identifier(), source);
        cmd.Blit(source, dest.Identifier());

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        rt.Release();
    }

    public void SetUp(RenderTargetIdentifier source, RenderTargetHandle dest)
    {
        this.source = source;
        this.dest = dest;
    }

}