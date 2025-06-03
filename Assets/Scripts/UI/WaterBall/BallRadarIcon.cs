using UnityEngine;
using UnityEngine.UI;

public class BallRadarIcon : MonoBehaviour
{
    public Transform player;
    public Transform ball;
    public RectTransform radarIcon;
    public float radarSize = 100f;
    public float mapRadius = 50f; // real world meters

    void Update()
    {
        Vector3 offset = ball.position - player.position;
        Vector2 flat = new Vector2(offset.x, offset.z);

        Vector2 radarPos = flat / mapRadius * radarSize;
        radarPos = Vector2.ClampMagnitude(radarPos, radarSize);

        radarIcon.anchoredPosition = radarPos;
    }
}

