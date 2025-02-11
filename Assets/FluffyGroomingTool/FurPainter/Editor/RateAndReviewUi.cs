using System;
using UnityEditor;
using UnityEngine;

/**
 * The logic should be as follows:
 * 
 * 1. Does not show up the first time the Editor is launched.
 * 2. Shows up the next time the Editor is launched. If the 50% of the poster is visible on
 * screen we count it as shown for that editor session.
 * 3. If it has been shown in 3 editor sessions, we stop showing it.
 * 4. If the user interacts with it by clicking any of the buttons, we stop showing it as well.
 */
namespace FluffyGroomingTool {
    public class RateAndReviewUI {
        private readonly Lazy<Texture2D> backDrop2 = new Lazy<Texture2D>(() => Resources.Load<Texture2D>("fluffy_review_backdrop"));
        private readonly GUIStyle invisibleButtonStyle = new GUIStyle();

        private static readonly float MIN_WIDTH = 674f;
        private static readonly float IMAGE_ASPECT_RATIO = MIN_WIDTH / 1200f;

        private static readonly string ASSET_STORE_LINK = "https://u3d.as/2uyA";
        private static readonly string VIEWS_KEY = "VIEWS_KEY";
        private static readonly string RATE_SESSION_ID = "RATE_SESSION_ID";
        private static readonly int MAX_NUMBER_OF_VIEWS = 3;
        private static readonly float RATE_BUTTON_SCALE = 0.26f;
        private static readonly float RATE_BUTTON_WIDTH = 320 * RATE_BUTTON_SCALE;
        private static readonly float RATE_BUTTON_HEIGHT = 150f * RATE_BUTTON_SCALE;
        private static readonly float FEEDBACK_BUTTON_WIDTH = 400;
        private static readonly float FEEDBACK_BUTTON_HEIGHT = 100;
        private static readonly float RATE_BUTTON_Y_POS_IN_PERCENT_OF_BACKGROUND_HEIGHT = 0.815f;

        private readonly EditorDeltaTime editorDeltaTime = new EditorDeltaTime();
        private static int numberOfViews = -1;

        private readonly ImportantButton rateButton = new ImportantButton() {
            positionRect = new Rect(),
            resource = "rate_button",
            gradientResource = "rate_button_gradient",
            fillColor = new Color(144 / 256f, 165 / 256f, 204 / 256f, 1f),
            clickAction = delegate {
                Application.OpenURL(ASSET_STORE_LINK);
                disableRateDialog();
            }
        };

        private static void disableRateDialog() {
            PerProjectPreferences.setInt(VIEWS_KEY, MAX_NUMBER_OF_VIEWS + 1);
            numberOfViews = MAX_NUMBER_OF_VIEWS + 1;
        }


        public void drawRateAndReviewUI(FluffyWindow fluffyWindow) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(1);
            var backDropRect = createBackDropRect(fluffyWindow);
            handleNumberOfViews(fluffyWindow, backDropRect);

            if (numberOfViews < 1 || numberOfViews > MAX_NUMBER_OF_VIEWS) {
                drawDiscordAndDocumentationUI(fluffyWindow);
                return;
            }

            EditorGUILayout.Space(backDropRect.width * IMAGE_ASPECT_RATIO);
            EditorGUI.DrawPreviewTexture(backDropRect, backDrop2.Value);
            var rateButtonPositionRect = createRateButtonRect(backDropRect);
            rateButton.positionRect = rateButtonPositionRect;
            editorDeltaTime.Update();
            rateButton.update(editorDeltaTime.deltaTime);
            rateButton.draw();
            var feedbackRect = createFeedbackButtonRect(rateButtonPositionRect);
            if (GUI.Button(feedbackRect, "", invisibleButtonStyle)) {
                Application.OpenURL(FluffyStartScreen.DISCORD_LINK);
                disableRateDialog();
            }

            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            drawDiscordAndDocumentationUI(fluffyWindow);
        }

        private static void drawDiscordAndDocumentationUI(FluffyWindow fluffyWindow) {
            EditorGUILayout.EndVertical();
            GUILayout.BeginVertical(fluffyWindow.brushPropertiesUI.PanelStyle);
            if (GUILayout.Button("Documentation", fluffyWindow.painterLayersUI.buttonStyle)) {
                Application.OpenURL(FluffyStartScreen.DOCUMENTATION_LINK);
            }

            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            if (GUILayout.Button("Discord", fluffyWindow.painterLayersUI.buttonStyle)) {
                Application.OpenURL(FluffyStartScreen.DISCORD_LINK);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }

        private void handleNumberOfViews(FluffyWindow fluffyWindow, Rect backDropRect) {
            numberOfViews = numberOfViews == -1 ? PerProjectPreferences.getInt(VIEWS_KEY, 0) : numberOfViews;
            if (
                isNewSession() &&
                backDropRect.y > 0 &&
                fluffyWindow.scroll.y + fluffyWindow.position.height > backDropRect.y + backDropRect.height / 2 ||
                numberOfViews == 0
            ) {
                PlayerPrefs.SetInt(RATE_SESSION_ID, PlayerPrefs.GetInt(FurStartScreenLauncher.SESSION_ID));
                PerProjectPreferences.setInt(VIEWS_KEY, numberOfViews + 1);
                PlayerPrefs.Save();
            }
        }

        private bool isNewSession() {
            return PlayerPrefs.GetInt(FurStartScreenLauncher.SESSION_ID, -1) != PlayerPrefs.GetInt(RATE_SESSION_ID, 1);
        }

        private Rect createRateButtonRect(Rect backDropRect) {
            return new Rect(
                backDropRect.x + backDropRect.width / 2f - RATE_BUTTON_WIDTH / 2f,
                backDropRect.y + backDropRect.height * RATE_BUTTON_Y_POS_IN_PERCENT_OF_BACKGROUND_HEIGHT - RATE_BUTTON_HEIGHT / 2f,
                RATE_BUTTON_WIDTH,
                RATE_BUTTON_HEIGHT
            );
        }

        private Rect createFeedbackButtonRect(Rect rateRect) {
            return new Rect(
                rateRect.x + rateRect.width / 2f - FEEDBACK_BUTTON_WIDTH / 2f,
                rateRect.y + rateRect.height + 10,
                FEEDBACK_BUTTON_WIDTH,
                FEEDBACK_BUTTON_HEIGHT
            );
        }

        private float lastRectY;

        private Rect createBackDropRect(FluffyWindow fluffyWindow) {
            var lasRectY = GUILayoutUtility.GetLastRect().y;
            var curLasRectY = lasRectY;
            lastRectY = curLasRectY == 0 ? lastRectY : curLasRectY;
            if (fluffyWindow.position.width < MIN_WIDTH) {
                return new Rect(
                    fluffyWindow.position.width / 2f - MIN_WIDTH / 2f,
                    lastRectY,
                    MIN_WIDTH,
                    MIN_WIDTH * IMAGE_ASPECT_RATIO
                );
            }

            return new Rect(
                0,
                lastRectY,
                fluffyWindow.position.width,
                fluffyWindow.position.width * IMAGE_ASPECT_RATIO
            );
        }
    }
}