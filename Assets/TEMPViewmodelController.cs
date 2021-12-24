using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class TEMPViewmodelController : MonoBehaviour
{
    public Transform cameraAxis;
    public Transform gunParent;
    public Vector3 normalPosition;
    public Vector3 normalRotation;
    
    public Vector3 aimPosition;
    public Vector3 aimRotation;
    public float aimFOV;
    public Vector3 crouchPosition;
    public Vector3 crouchRotation;
    private MovementUtils u;
    private Vector3 targetPosition;
    private Vector3 targetRotation;
    private Vector3 prevWorldRotation;
    private Vector3 targetRotationOffset;
    private Vector3 rotationOffset;
    private Vector3 prevPosition;
    public CinemachineVirtualCamera cam;
    private float normalFOV;
    void Start() {
        u = GetComponent<MovementUtils>();
        targetPosition = normalPosition;
        targetRotation = normalRotation;
        u.onEnterGround += () => {
            if (!Physics.Raycast(prevPosition, Vector3.down, u.capsuleCollider.height / 2 + 0.006f, LayerMask.GetMask("Default")) && !Input.GetMouseButton(1)) {
                targetRotationOffset += new Vector3(40, 0, 0);
            }
        };
        u.onJump += () => {
            if (!Input.GetMouseButton(1))
                targetRotationOffset += new Vector3(40, 0, 0);
        };
        normalFOV = cam.m_Lens.FieldOfView;
    }

    Vector3 AngleLerp(Vector3 startAngle, Vector3 finishAngle, float t)
    {        
        float xLerp = Mathf.LerpAngle(startAngle.x, finishAngle.x, t);
        float yLerp = Mathf.LerpAngle(startAngle.y, finishAngle.y, t);
        float zLerp = Mathf.LerpAngle(startAngle.z, finishAngle.z, t);
        return new Vector3(xLerp, yLerp, zLerp);
    }

    void Update() {
        if (!Input.GetMouseButton(1)) {
            if (u.crouchInput) {
                targetPosition = crouchPosition;
                targetRotation = crouchRotation;
            } else {
                targetPosition = normalPosition;
                targetRotation = normalRotation;
            }
        } else {
            targetPosition = aimPosition;
            targetRotation = aimRotation;
        }

        gunParent.localPosition = Vector3.Lerp(gunParent.localPosition, targetPosition, Time.deltaTime * 10);
        
        // gun sway
        Vector3 worldRotation = cameraAxis.eulerAngles;
        Vector3 worldRotationDiff = worldRotation - prevWorldRotation;
        prevWorldRotation = worldRotation;
        gunParent.localEulerAngles = AngleLerp(gunParent.localEulerAngles, targetRotation - AngleLerp(Vector3.zero, worldRotationDiff, Input.GetMouseButton(1) ? 0.1f : 0.5f) + rotationOffset, Time.deltaTime * 10);
        rotationOffset = AngleLerp(rotationOffset, targetRotationOffset, Time.deltaTime * 5);
        targetRotationOffset = AngleLerp(Vector3.zero, targetRotationOffset, Time.deltaTime * 10);

        prevPosition = transform.position;

        cam.m_Lens.FieldOfView = Mathf.Lerp(cam.m_Lens.FieldOfView, Input.GetMouseButton(1) ? aimFOV : normalFOV, Time.deltaTime * 10);

        if (Input.GetMouseButtonDown(0)) {
            targetRotationOffset += new Vector3(-10, 0, 0);
        }
    }
}
