using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetTrigger : MonoBehaviour
{

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.GetComponent<Rigidbody>().transform.localPosition = new Vector3(0, 5, 0);
            other.gameObject.GetComponent<Rigidbody>().transform.localRotation = new Quaternion(0, 0, 0, 0);
            other.gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        }
    }
}
