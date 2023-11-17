using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToastMessage : MonoBehaviour
{
    public static void Show(string message, int seconds = 3)
    {
        GameObject prefab = Resources.Load("Prefabs/Toast Message") as GameObject;
        GameObject go = Instantiate(prefab);
        Text text = go.GetComponentInChildren<Text>();
        text.text = message;
        Destroy(go, seconds);
    }
}
