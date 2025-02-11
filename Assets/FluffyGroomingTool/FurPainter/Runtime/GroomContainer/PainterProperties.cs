using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterProperties : ScriptableObject {
        [SerializeField] public bool isBrushMenuExpanded = true;
        [SerializeField] public float brushSize = 0.1f;
        [SerializeField] public float brushFalloff = 0.5f;
        [SerializeField] public float brushOpacity = 0.5f;
        [SerializeField] public float brushFalloffPercentOfBrushSize = 0.5f;
        [SerializeField] public bool isNormalIgnored;
        [SerializeField] public bool isSelectionLocked;
        [SerializeField] public bool isMirrorMode;
        [SerializeField] public int mirrorAxisTab;
        [SerializeField] public int type = (int) PaintType.HEIGHT;
        [Range(0, 1)] [SerializeField] public float painterMagnitudeIntensity = 1f;
        [Range(0, 1)] [SerializeField] public float clumpMaskIntensity;

        [SerializeField] public bool maskErase = true;
        [SerializeField] public bool isClumpTwistSelected;
        [SerializeField] public float twistAmount = 1f;
        [SerializeField] public float twistSpread = 0.3f;
        [SerializeField] public Color overrideColor = Color.black;
        [SerializeField] public float overrideIntensity = 1f;

        [SerializeField] public float resetWidthAmount = 0.5f;
        [SerializeField] public float resetLengthAmount = 0.5f;
        [SerializeField] public float resetBendAmount = 0.5f;
        [SerializeField] public float resetOrientAmount = 0.5f;
        [SerializeField] public bool isGroomAllLayerAtOnce;

        public Vector4 getResetValuesAsVector() {
            return new Vector4(resetLengthAmount, resetWidthAmount, resetOrientAmount, resetBendAmount);
        }

        public float getMagnitudeIntensity() {
            return type == (int) PaintType.CLUMPING_MASK ? clumpMaskIntensity : painterMagnitudeIntensity;
        }
    }
}