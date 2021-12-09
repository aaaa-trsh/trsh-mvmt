using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIValueBind : MonoBehaviour
{
    private TMPro.TextMeshProUGUI text;
    public MonoBehaviour script;
    public string key;
    public string format;
    
    void Start() {
        text = GetComponent<TMPro.TextMeshProUGUI>();
    }

    void Update() {
        text.text = string.Format(
            format, 
            script.GetType()
                .GetProperty(key)
                .GetValue(script, null)
        );
    }
}
