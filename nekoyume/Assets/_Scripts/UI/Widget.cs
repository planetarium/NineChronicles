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

        private Animator _animator = null;
        private Material _glass = null;

        public static T Create<T>(bool activate = false) where T : Widget
        {
            if (CanvasObj == null)
            {
                CanvasObj = GameObject.FindGameObjectWithTag("Canvas");
            }
            Type t = typeof(T);
            string[] names = t.ToString().Split('.');
            string resname = $"Prefab/Widget/UI_{names[names.Length - 1]}";
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
                else if (twidget is HUD)
                {
                    go.transform.SetParent(CanvasObj.transform.Find("HUD"));
                    go.SetActive(activate);
                }
                else
                {
                    if (Dict.ContainsKey(t))
                    {
                        Debug.LogWarning($"Duplicated create widget: {t}");
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
            Debug.LogWarning(($"widget not exist: {t}"));
            return null;
        }

        public static T Find<T>() where T : Widget
        {
            Type t = typeof(T);
            GameObject go;
            if (Dict.TryGetValue(t, out go))
            {
                return go.GetComponent<T>();
            }
            return null;
        }

        private bool FindGlassMaterial(GameObject go)
        {
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (image && image.material && image.material.name == "Glass")
            {
                _glass = image.material;
                return true;
            }
            return false;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (!_animator)
            {
                _animator = GetComponent<Animator>();
            }
            if (_animator)
            {
                _animator.Play("Show");
            }
            if (!_glass)
            {
                FindGlassMaterial(gameObject);
            }
            if (_glass)
            {
                StartCoroutine(Blur());
            }
        }

        public virtual IEnumerator WaitForShow()
        {
            Show();
            while (gameObject.activeSelf)
            {
                yield return null;
            }
        }

        public virtual IEnumerator Blur()
        {
            if (!_glass)
            {
                yield break;
            }

            _glass.SetFloat("_Radius", 0.0f);
            float time = 0.0f;
            while (true)
            {
                float radius = Mathf.Lerp(0.0f, 6.0f, time);
                time += Time.deltaTime;
                yield return null;
                _glass.SetFloat("_Radius", radius);
                if (time > 1.0f)
                    break;

                if (!gameObject.activeInHierarchy)
                    break;
            }
        }

        public virtual void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            // TODO : wait close animation
            if (_animator)
            {
                _animator.Play("Close");
            }

            gameObject.SetActive(false);
        }

        public virtual bool IsActive()
        {
            return gameObject.activeSelf;
        }

        public void Toggle()
        {
            if (IsActive())
            {
                Close();
            }
            else
            {
                Show();
            }
        }
    }

    public class Popup : Widget
    {
        public override void Close()
        {
            Destroy(gameObject);
        }
    }

    public class HUD : Widget
    {
        public override void Close()
        {
            Destroy(gameObject);
        }
    }
}
