using UnityEngine;
using UnityEngine.UI;

public class BallDirectionIndicator : MonoBehaviour
{
    public Transform playerCamera;
    public Transform ball;
    public RectTransform indicatorUI;  // Arrow image
    public Canvas canvas;

    void Update()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(ball.position);
        bool onScreen = screenPoint.x > 0 && screenPoint.x < 1 &&
                        screenPoint.y > 0 && screenPoint.y < 1 && screenPoint.z > 0;

        indicatorUI.gameObject.SetActive(!onScreen);

        if (!onScreen)
        {
            Vector3 dir = (ball.position - playerCamera.position).normalized;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            indicatorUI.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}
