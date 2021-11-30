using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public bool paused = false;

    void Start() {
        if (paused) {
            Pause();
        } else {
            Unpause();
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (paused) {
                Unpause();
            } else {
                Pause();
            }
        }
    }
    
    void Pause() {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenu.SetActive(true);
        paused = true;
    }

    void Unpause() {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.SetActive(false);
        paused = false;
    }
}
