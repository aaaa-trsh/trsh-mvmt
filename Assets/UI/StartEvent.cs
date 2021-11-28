using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StartEvent : MonoBehaviour
{
    public UnityEvent _event;
    void Start() {
        _event.Invoke();
    }
    void OnEnable() {
        _event.Invoke();
    }
}
