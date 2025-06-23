#if GRIFFIN
#if __MICROSPLAT_POLARIS__ && __MICROSPLAT_STREAMS__
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using JBooth.MicroSplat;

namespace Pinwheel.Griffin.PaintTool
{
    public abstract class GMicroSplatStreamsPainterBase : IGTexturePainter, IConditionalPainter
    {
        public abstract GTextureChannel TargetChannel { get; }

        private static Material painterMaterial;
        public static Material PainterMaterial
        {
            get
            {
                if (painterMaterial == null)
                {
                    painterMaterial = new Material(GRuntimeSettings.Instance.internalShaders.maskPainterShader);
                }
                return painterMaterial;
            }
        }

        public string Instruction
        {
            get
            {
                string s = string.Format(
                    "Modify terrain mask.\n" +
                    "   - Hold Left Mouse to paint.\n" +
                    "   - Hold {0} & Left Mouse to erase.\n" +
                    "   - Hold {1} & Left Mouse to smooth.",
                    "Ctrl",
                    "Shift");
                return s;
            }
        }

        public string HistoryPrefix
        {
            get
            {
                return "Streams Painting";
            }
        }

        static List<GTerrainResourceFlag> undoResourcesFlags = new List<GTerrainResourceFlag> { GTerrainResourceFlag.StreamMap };

        public List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args)
        {
            return undoResourcesFlags;
        }

        private void SetupTextureGrid(GStylizedTerrain t, Material mat)
        {
            mat.SetTexture("_MainTex_Left",
                t.LeftNeighbor && t.LeftNeighbor.TerrainData ?
                t.LeftNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_TopLeft",
                t.LeftNeighbor && t.LeftNeighbor.TopNeighbor && t.LeftNeighbor.TopNeighbor.TerrainData ?
                t.LeftNeighbor.TopNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_TopLeft",
                t.TopNeighbor && t.TopNeighbor.LeftNeighbor && t.TopNeighbor.LeftNeighbor.TerrainData ?
                t.TopNeighbor.LeftNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Top",
                t.TopNeighbor && t.TopNeighbor.TerrainData ?
                t.TopNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_TopRight",
                t.RightNeighbor && t.RightNeighbor.TopNeighbor && t.RightNeighbor.TopNeighbor.TerrainData ?
                t.RightNeighbor.TopNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_TopRight",
                t.TopNeighbor && t.TopNeighbor.RightNeighbor && t.TopNeighbor.RightNeighbor.TerrainData ?
                t.TopNeighbor.RightNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Right",
                t.RightNeighbor && t.RightNeighbor.TerrainData ?
                t.RightNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_BottomRight",
                t.RightNeighbor && t.RightNeighbor.BottomNeighbor && t.RightNeighbor.BottomNeighbor.TerrainData ?
                t.RightNeighbor.BottomNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_BottomRight",
                t.BottomNeighbor && t.BottomNeighbor.RightNeighbor && t.BottomNeighbor.RightNeighbor.TerrainData ?
                t.BottomNeighbor.RightNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_Bottom",
                t.BottomNeighbor && t.BottomNeighbor.TerrainData ?
                t.BottomNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);

            mat.SetTexture("_MainTex_BottomLeft",
                t.LeftNeighbor && t.LeftNeighbor.BottomNeighbor && t.LeftNeighbor.BottomNeighbor.TerrainData ?
                t.LeftNeighbor.BottomNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);
            mat.SetTexture("_MainTex_BottomLeft",
                t.BottomNeighbor && t.BottomNeighbor.LeftNeighbor && t.BottomNeighbor.LeftNeighbor.TerrainData ?
                t.BottomNeighbor.LeftNeighbor.TerrainData.Shading.StreamMapOrDefault :
                Texture2D.blackTexture);
        }

        public void BeginPainting(GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (args.MouseEventType == GPainterMouseEventType.Down)
            {
                MicroSplatObject mso = terrain.GetComponent<MicroSplatObject>();
                if (mso != null)
                {
                    mso.streamTexture = terrain.TerrainData.Shading.StreamMapOrDefault;
                }
            }

            if (args.MouseEventType == GPainterMouseEventType.Up)
            {
                return;
            }

            Vector2[] uvCorners = GPaintToolUtilities.WorldToUvCorners(terrain, args.WorldPointCorners);
            Rect dirtyRect = GUtilities.GetRectContainsPoints(uvCorners);

            int resolution = terrain.TerrainData.Shading.StreamMapResolution;
            RenderTexture rt = GTerrainTexturePainter.Internal_GetRenderTexture(terrain, resolution);
            PaintOnRT(terrain, args, rt, uvCorners);

            RenderTexture.active = rt;
            terrain.TerrainData.Shading.StreamMap.ReadPixels(
                new Rect(0, 0, resolution, resolution), 0, 0);
            terrain.TerrainData.Shading.StreamMap.Apply();
            RenderTexture.active = null;

            if (args.ForceUpdateGeometry)
            {
                terrain.TerrainData.Geometry.SetRegionDirty(dirtyRect);
            }
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        private void PaintOnRT(GStylizedTerrain terrain, GTexturePainterArgs args, RenderTexture rt, Vector2[] uvCorners)
        {
            Texture2D bg = terrain.TerrainData.Shading.StreamMapOrDefault;
            GCommon.CopyToRT(bg, rt);

            Material mat = PainterMaterial;
            mat.SetTexture("_MainTex", bg);
            SetupTextureGrid(terrain, mat);
            mat.SetTexture("_Mask", args.BrushMask);
            mat.SetFloat("_Opacity", args.Opacity);
            Vector4 channel;
            if (TargetChannel == GTextureChannel.R)
            {
                channel = new Vector4(1, 0, 0, 0);
            }
            else if (TargetChannel == GTextureChannel.G)
            {
                channel = new Vector4(0, 1, 0, 0);
            }
            else if (TargetChannel == GTextureChannel.B)
            {
                channel = new Vector4(0, 0, 1, 0);
            }
            else
            {
                channel = new Vector4(0, 0, 0, 1);
            }
            mat.SetVector("_Channel", channel);
            args.ConditionalPaintingConfigs.SetupMaterial(terrain, mat);

            int pass =
                args.ActionType == GPainterActionType.Normal ? 0 :
                args.ActionType == GPainterActionType.Negative ? 1 :
                args.ActionType == GPainterActionType.Alternative ? 2 : 0;
            GCommon.DrawQuad(rt, uvCorners, mat, pass);
        }

        public void EndPainting(GStylizedTerrain terrain, GTexturePainterArgs args)
        {
            if (args.ForceUpdateGeometry)
            {
                terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);
            }
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        public class Wetness : GMicroSplatStreamsPainterBase
        {
            public override GTextureChannel TargetChannel => GTextureChannel.R;
        }

        public class Puddles : GMicroSplatStreamsPainterBase
        {
            public override GTextureChannel TargetChannel => GTextureChannel.G;
        }

        public class Streams : GMicroSplatStreamsPainterBase
        {
            public override GTextureChannel TargetChannel => GTextureChannel.B;
        }

        public class Lava : GMicroSplatStreamsPainterBase
        {
            public override GTextureChannel TargetChannel => GTextureChannel.A;
        }
    }
}
#endif
#endif
