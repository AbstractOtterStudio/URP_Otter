using UnityEngine;
using UnityEngine.UI;

public class WaterSoccerVisualOnlyUI : MonoBehaviour
{
    [Header("Required Game Objects")]
    public Transform ball;
    public Transform playerCamera;

    [Header("Prefabs")]
    public GameObject possessionMarkerPrefab;
    public Sprite ballArrowIcon;

    private Canvas canvas;

    void Start()
    {
        SetupCanvas();
        SetupBallDirectionUI();
        SetupPossessionMarker();
    }

    void SetupCanvas()
    {
        GameObject canvasGO = new GameObject("GameUI_VisualOnly");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    void SetupBallDirectionUI()
    {
        GameObject arrowUI = new GameObject("BallArrowUI");
        arrowUI.transform.SetParent(canvas.transform);

        RectTransform rt = arrowUI.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(60, 60);

        var img = arrowUI.AddComponent<Image>();
        img.sprite = ballArrowIcon;
        img.color = Color.white;

        var dir = gameObject.AddComponent<BallDirectionIndicator>();
        dir.ball = ball;
        dir.playerCamera = playerCamera;
        dir.indicatorUI = rt;
        dir.canvas = canvas;
    }

    void SetupPossessionMarker()
    {
        var marker = gameObject.AddComponent<PossessionMarker>();
        marker.ball = ball.GetComponent<Ball>();
        marker.markerPrefab = possessionMarkerPrefab;
    }
}
