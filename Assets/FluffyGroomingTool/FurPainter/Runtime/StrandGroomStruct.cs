using UnityEngine;

namespace FluffyGroomingTool {
    public struct StrandGroomStruct {
        public Vector2 flowDirectionRoot;
        public Vector2 flowDirectionBend;
        public Vector2 flowDirectionOrientation;

        public Vector2 scale;
        public readonly float raise;
        public readonly float isErased;
        public readonly float windContribution;
        public readonly float clumpMask;
        public Vector4 twist;
        public Vector4 overrideColor;

        public StrandGroomStruct(Vector2 flowDirectionRoot, Vector2 flowDirectionBend, Vector2 flowDirectionOrientation, Vector2 scale,
            float raise, float isErased, float windContribution, float clumpMask, Vector4 twist, Vector4 overrideColor) {
            this.flowDirectionRoot = flowDirectionRoot;
            this.flowDirectionBend = flowDirectionBend;
            this.flowDirectionOrientation = flowDirectionOrientation;
            this.scale = scale;
            this.raise = raise;
            this.isErased = isErased;
            this.windContribution = windContribution;
            this.clumpMask = clumpMask;
            this.twist = twist;
            this.overrideColor = overrideColor;
        }

        public static int size() {
            return
                sizeof(float) * 2 +
                sizeof(float) * 2 +
                sizeof(float) * 2 +
                sizeof(float) * 2 +
                sizeof(float) +
                sizeof(float) +
                sizeof(float) +
                sizeof(float) * 4 +
                sizeof(float) * 4 +
                sizeof(float);
        }

        public static StrandGroomStruct convertToStruct(StrandGroom strandGroom) {
            return new StrandGroomStruct(
                strandGroom.flowDirectionRoot,
                strandGroom.flowDirectionBend,
                strandGroom.flowDirectionOrientation,
                strandGroom.scale,
                strandGroom.raise,
                strandGroom.isErased,
                strandGroom.windContribution,
                strandGroom.clumpMask,
                strandGroom.twist,
                strandGroom.overrideColor
            );
        }
    }
}