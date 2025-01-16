using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * AbilityGrab.cs
 * 
 * Purpose: Implements a grabbing mechanic that allows the player to pick up
 * and release objects tagged as "Holdable" within range.
 * 
 * Features:
 * - Detects and tracks nearby grabbable objects
 * - Picks up the nearest object when activated
 * - Manages object physics states during grab/release
 * - Integrates with floating mechanics
 * 
 * Usage:
 * 1. Attach to player GameObject
 * 2. Assign a grab point Transform
 * 3. Ensure grabbable objects are tagged "Holdable"
 */
public class AbilityGrab : MonoBehaviour
{
    [SerializeField]
    Transform grabPoint;
    CapsuleCollider cc;
    Collider nearest = null;
    bool isGrabbing = false;

    private List<Collider> objectsInRange = new List<Collider>();

    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            if(isGrabbing)
            {
                ReleaseObject();
            }
            else
            {
                GrabNearestObject();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Holdable") || other is IGrabbable)
        {
            objectsInRange.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        objectsInRange.Remove(other);
    }

    private void GrabNearestObject()
    {
        if (objectsInRange.Count > 0)
        {
            float nearestDist = float.MaxValue;
            
            foreach(Collider col in objectsInRange)
            {
                float dist = Vector3.Distance(grabPoint.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearest = col;
                    nearestDist = dist;
                }
            }

            if (nearest != null)
            {
                Debug.Log($"Grabbing nearest object: {nearest.gameObject.name}");
                nearest.GetComponent<Rigidbody>().isKinematic = true;
                nearest.GetComponent<Collider>().enabled = false;
                nearest.GetComponent<KeepFloating>().isFloating = false;
                nearest.transform.position = grabPoint.position;
                nearest.transform.parent = grabPoint;
                isGrabbing = true;
                if (nearest.TryGetComponent<IGrabbable>(out IGrabbable grabbable))
                {
                    grabbable.OnGrab();
                }
            }
        }
    }

    private void ReleaseObject()
    {
        if (nearest != null)
        {
            nearest.GetComponent<Rigidbody>().isKinematic = false;
            nearest.GetComponent<Collider>().enabled = true;
            nearest.GetComponent<KeepFloating>().isFloating = true;

            if (nearest.TryGetComponent<IGrabbable>(out IGrabbable grabbable))
            {
                grabbable.OnRelease();
            }
            nearest.transform.parent = null;
            nearest = null;
            isGrabbing = false;
        }
    }
}
