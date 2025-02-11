using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class CardMeshProperties : ICloneable {
        [SerializeField] int _tempCardSubdivisionsY = -1;
        /**
         * Aplogies to myself and whoever reads this. This needs some refactoring. 
         */
        public int RedoWatcherCardSubdivisionsY { get; set; }
        public int TempSubdivisionsY {
            get {
                if (_tempCardSubdivisionsY == -1) _tempCardSubdivisionsY = cardSubdivisionsY;
                return _tempCardSubdivisionsY;
            }
            set => _tempCardSubdivisionsY = value;
        }

        public bool isSameCurve(float sum) {
            if (curveSum < 0) curveSum = sum;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (sum == curveSum) return true;
            curveSum = sum;
            return false;
        }

        [SerializeField] public int cardSubdivisionsX = 1;
        [SerializeField] public int cardSubdivisionsY = 4;
        private float curveSum = -1;

        [SerializeField] public AnimationCurve shapeCurve = new AnimationCurve(
            new Keyframe(0, 1f, -0.007165052f, -0.007165052f, 0, 0.6583334f),
            new Keyframe(1f, 0f, -16.49596f, -16.49596f, 0.05833334f, 0)
        );

        [SerializeField] public AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);


        public int getCardMeshVerticesY() {
            return cardSubdivisionsY + 1;
        }

        public object Clone() {
            return new CardMeshProperties() {
                TempSubdivisionsY = cardSubdivisionsY,
                cardSubdivisionsX = cardSubdivisionsX,
                shapeCurve = new AnimationCurve(shapeCurve.keys),
                moveCurve = new AnimationCurve(moveCurve.keys),
                curveSum = curveSum
            };
        }
    }
}