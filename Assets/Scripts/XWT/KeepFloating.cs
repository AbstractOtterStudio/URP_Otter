using UnityEngine;

public class KeepFloating : MonoBehaviour
{
    [SerializeField]
    float minY = -0.1f;
    [SerializeField]
    float maxY = 0.1f;
    public bool isFloating = true;
    private void Update()
    {
        if(isFloating)
        {
            transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, minY, maxY), transform.position.z);
        }
    }
}