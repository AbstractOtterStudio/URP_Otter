using UnityEngine;
using UnityEngine.UI;

public class BallWorldArrow : MonoBehaviour
{
    public Transform ball;
    public GameObject arrowPrefab;
    private GameObject arrow;

    void Start()
    {
        arrow = Instantiate(arrowPrefab);
    }

    void Update()
    {
        arrow.transform.position = ball.position + Vector3.up * 0.2f;
        Vector3 dir = ball.GetComponent<Rigidbody>().velocity.normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            arrow.transform.rotation = Quaternion.LookRotation(dir);
            arrow.SetActive(true);
        }
        else
        {
            arrow.SetActive(false);
        }
    }
}

