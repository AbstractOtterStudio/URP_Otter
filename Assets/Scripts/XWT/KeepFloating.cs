using UnityEngine;

/*
 * KeepFloating.cs
 * 
 * Purpose: Maintains objects within a specified vertical range to create
 * a floating effect, typically used for objects in water.
 * 
 * Features:
 * - Configurable minimum and maximum Y positions
 * - Can be toggled on/off
 * - Simple and efficient implementation
 * 
 * Usage:
 * 1. Attach to objects that should float
 * 2. Set min/max Y values for float range
 * 3. Toggle isFloating as needed
 */
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