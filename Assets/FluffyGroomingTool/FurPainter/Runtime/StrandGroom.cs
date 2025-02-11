using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class StrandGroom : ICloneable {
        public Vector2 flowDirectionRoot;
        public Vector2 flowDirectionBend;
        public Vector2 flowDirectionOrientation;
        public Vector2 scale;
        public float raise;
        public float isErased;
        public float windContribution;
        public float clumpMask;
        public Vector4 twist;
        public Vector4 overrideColor;

        public StrandGroom(Vector2 flowDirectionRoot, Vector2 flowDirectionBend, Vector2 flowDirectionOrientation, Vector2 scale,
            float raise, float isErased, float windContribution, float clumpMask, Vector4 twist, Color overrideColor) {
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

        public static StrandGroom zero() {
            var centered = new Vector2(0.49f, 0.49f);
            return new StrandGroom(centered, centered, centered, centered, 1f, 0, 1, 1, Vector2.zero, new Vector4(0f, 0f, 0f, 1f));
        }

        public static StrandGroom convertFromStruct(StrandGroomStruct strandGroomStruct) {
            return new StrandGroom(
                strandGroomStruct.flowDirectionRoot,
                strandGroomStruct.flowDirectionBend,
                strandGroomStruct.flowDirectionOrientation,
                strandGroomStruct.scale,
                strandGroomStruct.raise,
                strandGroomStruct.isErased,
                strandGroomStruct.windContribution,
                strandGroomStruct.clumpMask,
                strandGroomStruct.twist,
                strandGroomStruct.overrideColor
            );
        }

        public object Clone() {
            return new StrandGroom(
                flowDirectionRoot,
                flowDirectionBend,
                flowDirectionOrientation,
                scale,
                raise,
                0,
                windContribution,
                clumpMask,
                twist,
                overrideColor
            );
        }
    }
}