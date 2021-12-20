using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIValueBind : MonoBehaviour
{
    private TMPro.TextMeshProUGUI text;
    private Image image;
    private bool isText;
    public MonoBehaviour script;
    public string key;
    public string format;
    
    void Start() {
        text = GetComponent<TMPro.TextMeshProUGUI>();
        image = GetComponent<Image>();
        isText = text != null;
    }

    void Update() {
        if (isText) {
            text.text = string.Format(
                format, 
                script.GetType()
                    .GetProperty(key)
                    .GetValue(script, null)
            );
        }
        else {
            image.fillAmount = (float)script.GetType()
                .GetProperty(key)
                .GetValue(script, null);
        }
    }
}
