using UnityEngine;
using UnityEngine.UI;

public class PossessionMarker : MonoBehaviour
{
    public Ball ball;
    public GameObject markerPrefab;
    private GameObject marker;

    [SerializeField] private float lastOwnerLostTime = -10f;
    private Transform lastOwner;

    void Update()
    {
        if (ball.Owner != null)
        {
            lastOwner = ball.Owner.transform;
            lastOwnerLostTime = -10f;

            if (marker == null)
                marker = Instantiate(markerPrefab);

            marker.transform.position = lastOwner.position + Vector3.up * 2.5f;
            marker.SetActive(true);
        }
        else if (lastOwner != null)
        {
            if (lastOwnerLostTime < 0f)
                lastOwnerLostTime = Time.time;

            // 显示在上一个持球者头顶 1 秒
            if (Time.time - lastOwnerLostTime < 1f)
            {
                marker.transform.position = lastOwner.position + Vector3.up * 2.5f;
                marker.SetActive(true);
            }
            else
            {
                marker.SetActive(false);
                lastOwner = null;
            }
        }
        else if (marker != null)
        {
            marker.SetActive(false);
        }
    }
}
