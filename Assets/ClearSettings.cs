using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearSettings : MonoBehaviour
{
    public void Clear()
    {
        PlayerPrefs.DeleteAll();
    }
}
