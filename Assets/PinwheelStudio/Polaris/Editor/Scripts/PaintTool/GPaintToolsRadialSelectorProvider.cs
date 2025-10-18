#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace Pinwheel.Griffin.PaintTool
{
    public class GTexturePaintModeRadialSelectorProvider : IRadialSelectorProvider<GTexturePaintingMode>
    {
        public GTexturePaintingMode[] GetItemValues(object context = null)
        {
            List<GTexturePaintingMode> values = new List<GTexturePaintingMode>((GTexturePaintingMode[])Enum.GetValues(typeof(GTexturePaintingMode)));
            values.Remove(GTexturePaintingMode.Custom);
            return values.ToArray();
        }
        public Texture2D GetIcon(GTexturePaintingMode value)
        {
            Texture2D icon = GEditorSkin.Instance.GetTexture(value.ToString() + "Icon");
            if (icon == null)
            {
                icon = GEditorSkin.Instance.GetTexture("GearIcon");
            }
            return icon;
        }

        public string GetItemLabel(GTexturePaintingMode value)
        {
            return ObjectNames.NicifyVariableName(value.ToString());
        }

        public string GetItemDescription(GTexturePaintingMode value)
        {
            switch (value)
            {
                case GTexturePaintingMode.Elevation: return "Gradually modify terrain height";
                case GTexturePaintingMode.HeightSampling: return "Set terrain height to a pre-sampled value";
                case GTexturePaintingMode.Terrace: return "Paint terrace (step) effect on terrain";
                case GTexturePaintingMode.Remap: return "Remap elevation value by a curve";
                case GTexturePaintingMode.Noise: return "Raise or lower terrain surface using noise";
                case GTexturePaintingMode.SubDivision: return "Add more subdivision to a particular area of the terrain";
                case GTexturePaintingMode.Visibility: return "Mark a particular region of a terrain as visible or not";
                case GTexturePaintingMode.Albedo: return "Paint color on the terrain's Albedo map";
                case GTexturePaintingMode.Metallic: return "Paint on the terrain's Metallic map R channel";
                case GTexturePaintingMode.Smoothness: return "Paint on the terrain's Metallic map A channel";
                case GTexturePaintingMode.Splat: return "Paint blend weight on the terrain's Splat Control maps";
                case GTexturePaintingMode.Mask: return "Paint on te terrain's Mask map";
                default: return string.Empty;
            }
        }
    }

    public class GFoliagePaintModeRadialSelectorProvider : IRadialSelectorProvider<GFoliagePaintingMode>
    {
        public GFoliagePaintingMode[] GetItemValues(object context = null)
        {
            List<GFoliagePaintingMode> values = new List<GFoliagePaintingMode>((GFoliagePaintingMode[])Enum.GetValues(typeof(GFoliagePaintingMode)));
            values.Remove(GFoliagePaintingMode.Custom);
            return values.ToArray();
        }
        public Texture2D GetIcon(GFoliagePaintingMode value)
        {
            Texture2D icon = GEditorSkin.Instance.GetTexture(value.ToString() + "Icon");
            if (icon == null)
            {
                icon = GEditorSkin.Instance.GetTexture("GearIcon");
            }
            return icon;
        }

        public string GetItemLabel(GFoliagePaintingMode value)
        {
            return ObjectNames.NicifyVariableName(value.ToString());
        }

        public string GetItemDescription(GFoliagePaintingMode value)
        {
            switch (value)
            {
                case GFoliagePaintingMode.PaintTree: return "Paint tree on to the terrain";
                case GFoliagePaintingMode.ScaleTree: return "Scale tree instances";
                case GFoliagePaintingMode.PaintGrass: return "Paint grass onto the terrain";
                case GFoliagePaintingMode.ScaleGrass: return "Scale grass instances";
                default: return string.Empty;
            }
        }
    }

    public class GObjectPaintModeRadialSelectorProvider : IRadialSelectorProvider<GObjectPaintingMode>
    {
        public GObjectPaintingMode[] GetItemValues(object context = null)
        {
            List<GObjectPaintingMode> values = new List<GObjectPaintingMode>((GObjectPaintingMode[])Enum.GetValues(typeof(GObjectPaintingMode)));
            values.Remove(GObjectPaintingMode.Custom);
            return values.ToArray();
        }
        public Texture2D GetIcon(GObjectPaintingMode value)
        {
            Texture2D icon = GEditorSkin.Instance.GetTexture(value.ToString() + "ObjectIcon");
            if (icon == null)
            {
                icon = GEditorSkin.Instance.GetTexture("GearIcon");
            }
            return icon;
        }

        public string GetItemLabel(GObjectPaintingMode value)
        {
            return ObjectNames.NicifyVariableName(value.ToString());
        }

        public string GetItemDescription(GObjectPaintingMode value)
        {
            switch (value)
            {
                case GObjectPaintingMode.Spawn: return "Paint objects on to the terrain";
                case GObjectPaintingMode.Scale: return "Scale object instances";
                default: return string.Empty;
            }
        }
    }
}
#endif