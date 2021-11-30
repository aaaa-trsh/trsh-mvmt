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
[System.Serializable]
public class SettingValue {
    public SettingType type;
    // value based on type
    public int intValue;
    public float floatValue;
    public string stringValue;
}
public class SettingsWidget : MonoBehaviour
{
    public SettingType type;
    public string key;
    public MonoBehaviour uiObject;
    public SettingValue defaultValue;

    public void Start() {
        // set default value if no key found
        if (PlayerPrefs.HasKey(key) == false) {
            if (type == SettingType.Int) {
                PlayerPrefs.SetInt(key, defaultValue.intValue);
            } else if (type == SettingType.Float) {
                PlayerPrefs.SetFloat(key, defaultValue.floatValue);
            } else if (type == SettingType.String) {
                PlayerPrefs.SetString(key, defaultValue.stringValue);
            }
        }
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
