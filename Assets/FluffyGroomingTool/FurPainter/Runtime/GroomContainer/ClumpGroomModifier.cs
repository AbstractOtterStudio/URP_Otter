using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [Serializable]
    public class ClumpGroomModifier : ICloneable {
        [SerializeField] [Range(0f, 0.5f)] public float clumpsSpacing = 0.05f;

        [SerializeField] public AnimationCurve attractionCurve = new AnimationCurve(
            new Keyframe(0, 1f, 0.00592891034f, 0.00592891034f, 0, 0.397826046f),
            new Keyframe(1f, 0f, -0.0187702253f, -0.0187702253f, 0.628260851f, 0)
        );

        [SerializeField] public StrandGroom[] strandsGroom;
        public ComputeBuffer clumpGroomBuffer;
        public bool needsUpdate;
        private float curveSum = -1;
        public string id = Guid.NewGuid().ToString();

        public bool isSameCurve(float sum) {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (curveSum == -1f) curveSum = sum;

            if (sum != curveSum) {
                curveSum = sum;
                return false;
            }

            return true;
        }

        public void update() {
            if (needsUpdate && strandsGroom != null) {
                needsUpdate = false;
                clumpGroomBuffer?.Dispose();
                clumpGroomBuffer = new ComputeBuffer(strandsGroom.Length, StrandGroomStruct.size());
                clumpGroomBuffer.SetData(strandsGroom.ToList().Select(StrandGroomStruct.convertToStruct).ToArray());
            }
        }

        public void copyValuesFromComputeBufferToNativeObject() {
            if (clumpGroomBuffer != null) AsyncGPUReadback.Request(clumpGroomBuffer, OnCompleteReadback);
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request) {
            if (request.hasError) {
                Debug.Log("GPU readback error detected for clumps.");
                return;
            }

            var groomStructs = request.ToPersistentArray<StrandGroomStruct>();
            new Thread(() => {
                strandsGroom = groomStructs.Select(StrandGroom.convertFromStruct).ToArray();
                groomStructs.Dispose();
            }).Start();
        }

        public object Clone() {
            return new ClumpGroomModifier {
                clumpsSpacing = clumpsSpacing,
                attractionCurve = new AnimationCurve(attractionCurve.keys),
                strandsGroom = strandsGroom,
                needsUpdate = true,
                curveSum = curveSum,
                id = id = Guid.NewGuid().ToString()
            };
        }

        private float _tempClumpsSpacing = -1;

        public float TempClumpSpacing {
            get {
                if (_tempClumpsSpacing == -1f) _tempClumpsSpacing = clumpsSpacing;
                return _tempClumpsSpacing;
            }
            set => _tempClumpsSpacing = value;
        }

        public bool isDistanceBetweenClumpsUndoPerformed() {
            return Math.Abs(TempClumpSpacing - clumpsSpacing) > GroomLayer.FLOAT_COMPARISON_TOLERANCE;
        }
    }
}