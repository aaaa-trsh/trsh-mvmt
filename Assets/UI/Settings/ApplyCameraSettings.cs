using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ApplyCameraSettings : MonoBehaviour
{
    public Volume globalVolume;
    private Camera camera;
    private int motion_blur_enabled = 0;
    void Start() {
        camera = GetComponent<Camera>();
        motion_blur_enabled = PlayerPrefs.GetInt("motion_blur_enabled");
    }
    void Update() {
        if (PlayerPrefs.GetInt("motion_blur_enabled") != motion_blur_enabled) {
            motion_blur_enabled = PlayerPrefs.GetInt("motion_blur_enabled");
            globalVolume.sharedProfile.TryGet<MotionBlur>(out var motion_blur);
            if (motion_blur_enabled == 0) {
                motion_blur.active = true;
            } else {
                motion_blur.active = false;
            }
        }
    }
}
