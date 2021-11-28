using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SettingType {
    Int,
    Float,
    String
}
public class SettingsWidget : MonoBehaviour
{
    public SettingType type;
    public string key;
    public MonoBehaviour uiObject;

    public void Start() {
        Debug.Log(uiObject.GetType().ToString());
        switch (uiObject.GetType().ToString())
        {
            case "UnityEngine.UI.Slider":
                if (type == SettingType.Int) { ((Slider) uiObject).value = PlayerPrefs.GetInt(key); }
                else if (type == SettingType.Float) { ((Slider) uiObject).value = PlayerPrefs.GetFloat(key); }
                break;
            case "TMPro.TMP_Dropdown":
                if (type == SettingType.Int) { ((TMP_Dropdown) uiObject).value = PlayerPrefs.GetInt(key); }
                break;
            case "UnityEngine.UI.Dropdown":
                if (type == SettingType.Int) { ((Dropdown) uiObject).value = PlayerPrefs.GetInt(key); }
                break;
        }
    }

    public void SavePlayerPref()
    {
        var value = uiObject.GetType().GetProperty("value").GetValue(uiObject, null);
        switch (type)
        {
            case SettingType.Int:
                PlayerPrefs.SetInt(key, (int)(object)value);
                break;
            case SettingType.Float:
                PlayerPrefs.SetFloat(key, (float)(object)value);
                break;
            case SettingType.String:
                PlayerPrefs.SetString(key, (string)(object)value);
                break;
        }
    }
}
