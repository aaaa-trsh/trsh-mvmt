using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private float mouseSensitivity = 100f;
    public Transform cameraRig;
    public Cinemachine.CinemachineVirtualCamera virtualCamera;
    private float xRotation = 0f;
    private float targetDutch = 0f;
    private float targetHeight = 0f;
    private Cinemachine.CinemachineBasicMultiChannelPerlin vCamNoise;

    void Start() {
        targetHeight = cameraRig.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        vCamNoise = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
    }

    void Update()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("sens", mouseSensitivity);
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        Vector3 cameraRigEuler = cameraRig.localEulerAngles;
        cameraRig.localRotation = Quaternion.Slerp(
            cameraRig.localRotation,
            Quaternion.Euler(new Vector3(xRotation, cameraRig.localRotation.eulerAngles.y, targetDutch)),
            Time.deltaTime * 7f
        );
        cameraRig.localPosition = new Vector3(cameraRig.localPosition.x, Mathf.Lerp(cameraRig.localPosition.y, targetHeight, Time.deltaTime * 15f), cameraRig.localPosition.z);
        transform.Rotate(Vector3.up * mouseX);

        vCamNoise.m_FrequencyGain = Mathf.Lerp(vCamNoise.m_FrequencyGain, 1f, Time.deltaTime);
        vCamNoise.m_AmplitudeGain = Mathf.Lerp(vCamNoise.m_AmplitudeGain, 0f, Time.deltaTime);
    }

    public void SetTargetDutch(float dutch) {
        targetDutch = dutch;
    }

    public void SetTargetHeight(float height) {
        targetHeight = height;
    }

    public void Shake(float frequency, float amplitude) {
        vCamNoise.m_FrequencyGain = frequency;
        vCamNoise.m_AmplitudeGain = amplitude;
    }
}
