using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tempest : MonoBehaviour
{
    public Transform cameraAxis;
    public Transform hitVFX;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private MovementUtils u;
    
    void Start() {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        u = GetComponent<MovementUtils>();
    }
    void EnableInput() {
    }
    void Update() {
        RaycastHit hit;
        if (Input.GetMouseButtonUp(2)) {
            if (Physics.Raycast(cameraAxis.position, cameraAxis.forward, out hit, 30, LayerMask.GetMask("Default"))) {
                Vector3 vel = Vector3.Reflect(cameraAxis.forward, hit.normal) * rb.velocity.magnitude * 1.2f;
                Physics.ComputePenetration(capsuleCollider, hit.point, Quaternion.identity, hit.collider, hit.collider.transform.position, hit.collider.transform.rotation, out Vector3 direction, out float distance);
                rb.position = hit.point + direction*distance;
                rb.velocity = vel;
                Invoke("EnableInput", 0.3f);
            }
            hitVFX.gameObject.SetActive(false);
        }

        if (Input.GetMouseButton(2)) {
            if (Physics.Raycast(cameraAxis.position, cameraAxis.forward, out hit, Mathf.Infinity, LayerMask.GetMask("Default"))) {
                hitVFX.gameObject.SetActive(true);
                hitVFX.position = hit.point;
                hitVFX.rotation = Quaternion.LookRotation(hit.normal);
                hitVFX.GetComponentInChildren<LineRenderer>().SetPosition(0, hit.point);
                hitVFX.GetComponentInChildren<LineRenderer>().SetPosition(1, hit.point + Vector3.Reflect(cameraAxis.forward, hit.normal) * 5 * 0.55f);
                hitVFX.GetComponentInChildren<LineRenderer>().SetPosition(2, hit.point + Vector3.Reflect(cameraAxis.forward, hit.normal) * 5 * 0.6f);
                hitVFX.GetComponentInChildren<LineRenderer>().SetPosition(3, hit.point + Vector3.Reflect(cameraAxis.forward, hit.normal) * 5);
                if (hit.distance > 30) {
                    hitVFX.GetComponentInChildren<Renderer>().material.SetColor("_TintColor", Color.red);
                    hitVFX.GetComponentInChildren<LineRenderer>().material.SetColor("_TintColor", Color.red);
                } else {
                    hitVFX.GetComponentInChildren<Renderer>().material.SetColor("_TintColor", Color.white);
                    hitVFX.GetComponentInChildren<LineRenderer>().material.SetColor("_TintColor", Color.white);
                }
            }
        }
    }
}
