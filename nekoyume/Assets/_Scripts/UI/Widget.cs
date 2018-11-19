using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.UI
{
    public class Widget : MonoBehaviour
    {
        private static GameObject CanvasObj = null;
        private static Dictionary<Type, GameObject> Dict = new Dictionary<Type, GameObject>();

        static public T Create<T>(bool activate = false) where T : Widget
        {
            if (CanvasObj == null)
            {
                CanvasObj = GameObject.FindGameObjectWithTag("Canvas");
            }
            Type t = typeof(T);
            string[] names = t.ToString().Split('.');
            string resname = string.Format("Prefab/Widget/{0}", names[names.Length - 1]);
            GameObject res = Resources.Load<GameObject>(resname);
            if (res != null)
            {
                GameObject go = GameObject.Instantiate(res, CanvasObj.transform);
                T twidget = go.GetComponent<T>();
                if (twidget is Popup)
                {
                    go.transform.SetParent(CanvasObj.transform.Find("Popup"));
                    go.SetActive(activate);
                }
                else
                {
                    if (Dict.ContainsKey(t))
                    {
                        Debug.LogWarning($"Duplicated create widget: {t.ToString()}");
                        Destroy(go);
                        Dict[t].SetActive(activate);
                        return Dict[t].GetComponent<T>();
                    }
                    go.transform.SetParent(CanvasObj.transform.Find("Widget"));
                    go.SetActive(activate);
                    Dict.Add(t, go);
                }
                return twidget;
            }
            return null;
        }

        static public T Find<T>() where T : Widget
        {
            Type t = typeof(T);
            GameObject go;
            if (Dict.TryGetValue(t, out go))
            {
                return go.GetComponent<T>();
            }
            return null;
        }

        private void Awake()
        {
        }

        virtual public void Show()
        {
            gameObject.SetActive(true);
        }

        virtual public IEnumerator WaitForShow()
        {
            Show();
            while (gameObject.activeSelf)
            {
                yield return null;
            }
        }

        virtual public void Close()
        {
            gameObject.SetActive(false);
        }
    }

    public class Popup : Widget
    {
        override public void Close()
        {
            Destroy(gameObject);
        }
    }
}
