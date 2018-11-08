using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.UI
{
    public class Widget : MonoBehaviour
    {
        public static List<GameObject> List = new List<GameObject>();
        private static GameObject CanvasObj = null;

        static public T Create<T>() where T : Widget
        {
            if (CanvasObj == null)
            {
                CanvasObj = GameObject.FindGameObjectWithTag("Canvas");
            }
            System.Type type = typeof(T);
            string[] names = type.ToString().Split('.');
            string resname = string.Format("Prefab/Widget/{0}", names[names.Length - 1]);
            GameObject res = Resources.Load<GameObject>(resname);
            if (res != null)
            {
                
                GameObject go = GameObject.Instantiate(res, CanvasObj.transform);
                T t = go.GetComponent<T>();
                if (t is Popup)
                {
                    go.transform.SetParent(CanvasObj.transform.Find("Popup"));
                    go.SetActive(false);
                }
                else
                {
                    go.transform.SetParent(CanvasObj.transform.Find("Widget"));
                }
                return t;
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
