using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePass : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<PlayerStateController>())
        {
            if (collision.gameObject.GetComponent<PlayerStateController>().canPassGame)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }
}
