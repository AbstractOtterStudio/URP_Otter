using UnityEngine;

namespace FluffyGroomingTool {
    public static class PaintVertexHelper {
        static float ALMOST_ZERO = 0.00001f;

        public static void paintVertex(this FluffyWindow f) {
            if (f.furCreator && f.activeObject) {
                if (f.furCreator.FurRenderer.isReadyToRender()) {
                    f.iterateVerticesAndPaint();
                    f.addOrDeleteFur();
                    f.incrementBrushMoveDistance();
                }
                else {
                    f.reset();
                }
            }
            else {
                f.reset();
            }
        }

        private static void reset(this FluffyWindow f) {
            f.OnSelectionChange();
            f.previousRayHit = Vector3.zero;
            f.previousMirrorRayHit = Vector3.zero;
        }

        private static void iterateVerticesAndPaint(
            this FluffyWindow f
        ) {
            if (f.previousRayHit == Vector3.zero) f.previousRayHit = f.mirroredRayCastHitMp.sourceVertex;
            if (f.previousMirrorRayHit == Vector3.zero) f.previousMirrorRayHit = f.mirroredRayCastHitMp.sourceVertex;
            int type = f.furCreator.getPainterProperties().type;
            if (type == (int) PaintType.ATTRACT) {
                if (f.clickStartHitPoint == null) f.clickStartHitPoint = f.rayCastHitMp.sourceVertex;
                if (f.mirrorClickStartHitPoint == null) f.mirrorClickStartHitPoint = f.mirroredRayCastHitMp.sourceVertex;
            }

            if (!isDirectionalPaintWithoutMovement(f)) {
                f.furCreator.paintGroom(type, f.rayCastHitMp, f.previousRayHit, f.clickStartHitPoint.GetValueOrDefault(),
                    type == (int) PaintType.SMOOTH);
                if (f.furCreator.getPainterProperties().isMirrorMode) {
                    f.furCreator.paintGroom(type, f.mirroredRayCastHitMp, f.previousMirrorRayHit, f.mirrorClickStartHitPoint.GetValueOrDefault(),
                        type == (int) PaintType.SMOOTH);
                }
            }
        }

        private static bool isDirectionalPaintWithoutMovement(FluffyWindow f) {
            var type = f.furCreator.getPainterProperties().type;
            return ((type == (int) PaintType.DIRECTION_BEND || type == (int) PaintType.DIRECTION_ORIENTATION ||
                     type == (int) PaintType.DIRECTION_ROOT) &&
                    (f.previousRayHit == f.rayCastHitMp.sourceVertex || f.previousMirrorRayHit == f.mirroredRayCastHitMp.sourceVertex)) ||
                   type == (int) PaintType.ATTRACT && f.clickStartHitPoint == f.rayCastHitMp.sourceVertex;
        }

        private static void incrementBrushMoveDistance(this FluffyWindow f) {
            if (!f.previousRayHit.isEqualTo(f.rayCastHitMp.sourceVertex)) {
                f.brushMoveDistance += Vector3.Distance(f.rayCastHitMp.sourceVertex, f.previousRayHit);
                if (f.brushMoveDistance > f.painterAddSpacingUI.paintDistanceBetweenStrands) {
                    f.brushMoveDistance = 0;
                }

                f.previousRayHit = f.rayCastHitMp.sourceVertex;
                f.previousMirrorRayHit = f.mirroredRayCastHitMp.sourceVertex;
            }
        }

        private static void addOrDeleteFur(this FluffyWindow f) {
            if (f.furCreator.getPainterProperties().type == (int) PaintType.ADD_FUR) { }
        }


        private static bool isEqualTo(this Vector3 a, Vector3 b) {
            return Vector3.SqrMagnitude(a - b) < ALMOST_ZERO;
        }
    }
}