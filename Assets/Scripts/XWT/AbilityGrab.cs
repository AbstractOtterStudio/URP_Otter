using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
        if (other.CompareTag("Holdable"))
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
                nearest.GetComponent<BoxCollider>().enabled = false;
                nearest.GetComponent<KeepFloating>().isFloating = false;
                nearest.transform.position = grabPoint.position;
                nearest.transform.parent = grabPoint;
                isGrabbing = true;
            }
        }
    }

    private void ReleaseObject()
    {
        if (nearest != null)
        {
            nearest.GetComponent<Rigidbody>().isKinematic = false;
            nearest.GetComponent<BoxCollider>().enabled = true;
            nearest.GetComponent<KeepFloating>().isFloating = true;
            nearest.transform.parent = null;
            nearest = null;
            isGrabbing = false;
        }
    }
}
