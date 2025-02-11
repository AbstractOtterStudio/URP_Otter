namespace FluffyGroomingTool
{
    public class FluffyTooltips
    {
        public const string Length = "Paint the length of the strands. The painted length is a value between 0 - 1 determined by the Layers height range.";
        public const string Width = "Paint the width of the strands. The painted width is a value between 0 - 1 determined by the Layers width range.";
        public const string RotateRoot = "Rotates the root of strand/card. Note that this is mostly relevant when using card based rendering and keep in mind that the Random Rotation setting of the layer will overwrite this setting.";
        public const string Bend = "Bends the strands keeping the root unchanged.";
        public const string Orient = "Orients the strand. Unlike bend this rotates from the root of the strand";
        public const string Twist = "Twist the strands or Clumps. A high layer subdivision count works best when using this effect.";
        public const string Attract = "Attracts the strands towards the clicked mouse position.";

        public const string Smooth = "Smooths the strands. You choose which properties should be affected by the smoothing by changing the slider values below.";
        public const string Reset = "Resets the groom back to its initial state. You choose which properties should be reset by adjusting the sliders below.";
        public const string Mask = "Hide or show strands.";
        public const string ClumpingMask = "Paint how much the strands should be attracted by the Clump Layer.";
        public const string Raise = "Tilts the strand towards the surface. This works best for card based rendering where the card root direction has been set up.";
        public const string Wind = "Paint how much the strands should be affected by wind and motion.";
        public const string OverrideColor = "Override the color of the material. Requires a material that supports vertex colors.";
    }
}