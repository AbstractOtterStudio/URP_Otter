using System;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterResetAndSmoothUI {
        public static float DEFAULT_MARGIN_TOP = 25;
        public static float DEFAULT_CHILD_VERTICAL_MARGIN = 5;

        public void drawResetAndSmoothUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            if (furCreator.painterProperties.type == (int) PaintType.RESET) {
                addResetUI(brushMenu, furCreator);
            }

            if (furCreator.painterProperties.type == (int) PaintType.SMOOTH) {
                addSmoothUI(brushMenu, furCreator);
            }
        }

        private void addSmoothUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
            furCreator.painterProperties.resetLengthAmount = furCreator.groomContainer.PainterProperties.undoSlider(
                new GUIContent("Smooth length amount:", "How much smoothing should be applied to the length?"),
                furCreator.painterProperties.resetLengthAmount
            );
            furCreator.painterProperties.resetWidthAmount = furCreator.groomContainer.PainterProperties.undoSlider(
                new GUIContent("Smooth width amount:", "How much smoothing should be applied to the width?"),
                furCreator.painterProperties.resetWidthAmount
            );
            furCreator.painterProperties.resetOrientAmount = furCreator.groomContainer.PainterProperties.undoSlider(
                new GUIContent("Smooth orientation amount:", "How much smoothing should be applied to the orientation?"),
                furCreator.painterProperties.resetOrientAmount
            );
            furCreator.painterProperties.resetBendAmount = furCreator.groomContainer.PainterProperties.undoSlider(
                new GUIContent("Smooth bend amount:", "How much smoothing should be applied to the bending?"),
                furCreator.painterProperties.resetBendAmount
            );
            GUILayout.EndVertical();
        }

        private void addResetUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
            furCreator.painterProperties.resetLengthAmount = furCreator.undoSlider(
                new GUIContent("Reset length amount:", "How much should the length be reset?"),
                furCreator.painterProperties.resetLengthAmount
            );
            furCreator.painterProperties.resetWidthAmount = furCreator.undoSlider(
                new GUIContent("Reset width amount:", "How much should the width be reset?"),
                furCreator.painterProperties.resetWidthAmount
            );
            furCreator.painterProperties.resetOrientAmount = furCreator.undoSlider(
                new GUIContent("Reset orientation amount:", "How much should the orientation be reset?"),
                furCreator.painterProperties.resetOrientAmount
            );
            furCreator.painterProperties.resetBendAmount = furCreator.undoSlider(
                new GUIContent("Reset bend amount:", "How much should bending be reset?"),
                furCreator.painterProperties.resetBendAmount
            );
            GUILayout.EndVertical();
        }
    }
}