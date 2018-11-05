using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


namespace Nekoyume.Network
{
    public class NetworkManager : MonoBehaviour
    {
        static public NetworkManager Instance = null;

        public string server = "http://localhost:4000/";
        public float interval = 1.0f;
        public string privateKey = "";

        public List<Request.Base> requests = new List<Request.Base>();
        public int requestCount = 0;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            privateKey = PlayerPrefs.GetString("private_key", "");
        }

        private void Start ()
        {
            StartCoroutine(UpdateRequest());
        }

        private IEnumerator UpdateRequest()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                yield return new WaitForEndOfFrame();

                requestCount = requests.Count;

                if (requests.Count > 0)
                {
                    Request.Base req = requests[0];
                    requests.RemoveAt(0);

                    if (string.IsNullOrEmpty(privateKey))
                    {
                        Debug.LogError("Private key is null");
                        continue;
                    }

                    if (req.Method == "post")
                    {
                        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
                        {
                            new MultipartFormDataSection("private_key", privateKey)
                        };
                        var members = req.GetType().GetMembers();
                        foreach (var member in members)
                        {
                            if (member.MemberType == System.Reflection.MemberTypes.Field)
                            {
                                System.Reflection.FieldInfo field = (System.Reflection.FieldInfo)member;
                                Debug.Log(member.Name);
                                Debug.Log(field.GetValue(req));
                                try
                                {
                                    formData.Add(new MultipartFormDataSection(member.Name, (string)field.GetValue(req)));
                                }
                                catch (ArgumentException)
                                {
                                    Debug.Log(member.Name);
                                }
                            }
                        }
                        UnityWebRequest w = UnityWebRequest.Post(server + req.Route, formData);
                        yield return w.SendWebRequest();
                        req.DataHandle(w.downloadHandler.text);
                    }
                    else
                    {
                        UnityWebRequest w = UnityWebRequest.Get(server + req.Route);
                        yield return w.SendWebRequest();
                        req.DataHandle(w.downloadHandler.text);
                    }
                }
            }
        }

        public void Push(Request.Base request)
        {
            requests.Add(request);
        }

        public void First(Request.Base request)
        {
            requests.Insert(0, request);
        }

        public string GeneratePrivateKey()
        {
            var key = Planetarium.Crypto.Keys.PrivateKey.Generate();
            return System.BitConverter.ToString(key.Bytes).Replace("-", "").ToLower();
        }
    }
}
