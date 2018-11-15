using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Init : MonoBehaviour
{
    private string version;

    private void Awake()
    {
        Assets.SimpleLocalization.LocalizationManager.Read();
    }

    void Start()
    {
        version = PlayerPrefs.GetString("version", "");
        StartCoroutine(CheckVersion(version));
    }

    IEnumerator CheckVersion(string clientVersion)
    {
        UnityWebRequest www = UnityWebRequest.Get("http://dev.nekoyu.me/version/");
        yield return www.SendWebRequest();
        if (www.error == null)
        {
            string serverVersion = www.downloadHandler.text;
            Debug.Log(serverVersion);
            if (serverVersion != clientVersion)
            {
                PlayerPrefs.SetString("version", serverVersion);
            }
        }
    }
}
