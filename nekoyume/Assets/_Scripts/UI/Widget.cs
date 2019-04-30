using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.UI
{
    public class Widget : MonoBehaviour
    {
        private static readonly Dictionary<Type, GameObject> Dict = new Dictionary<Type, GameObject>();
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        private Animator _animator;
        private Material _glass;
        private bool _animCloseEnd;
        
        public RectTransform RectTransform { get; private set; }

        protected virtual void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public static T Create<T>(bool activate = false) where T : Widget
        {
            var t = typeof(T);
            var names = t.ToString().Split('.');
            var resName = $"UI/Prefabs/UI_{names[names.Length - 1]}";
            var res = Resources.Load<GameObject>(resName);
            if (res != null)
            {
                var go = Instantiate(res, MainCanvas.instance.transform);
                var widget = go.GetComponent<T>();
                if (widget is PopupWidget)
                {
                    go.transform.SetParent(MainCanvas.instance.popup.transform);
                    go.SetActive(activate);
                }
                else if (widget is HudWidget)
                {
                    go.transform.SetParent(MainCanvas.instance.hud.transform);
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
                    go.transform.SetParent(MainCanvas.instance.widget.transform);
                    go.SetActive(activate);
                    Dict.Add(t, go);
                }
                return widget;
            }
            Debug.LogWarning(($"widget not exist: {t}"));
            return null;
        }

        public static T Find<T>() where T : Widget
        {
            var t = typeof(T);
            GameObject go;
            return Dict.TryGetValue(t, out go) ? go.GetComponent<T>() : null;
        }

        private void FindGlassMaterial(GameObject go)
        {
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (!image || !image.material || image.material.name != "Glass")
            {
                return;
            }
            
            _glass = image.material;
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

        protected virtual IEnumerator Blur()
        {
            if (!_glass)
            {
                yield break;
            }

            _glass.SetFloat(Radius, 0f);
            var time = 0.0f;
            while (true)
            {
                var radius = Mathf.Lerp(0f, 6f, time);
                time += Time.deltaTime * 2f;
                yield return null;
                _glass.SetFloat(Radius, radius);
                if (time > 1.0f)
                    break;

                if (!gameObject.activeInHierarchy)
                    break;
            }
            
            _glass.SetFloat(Radius, 6f);
        }

        public virtual void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            // TODO : wait close animation
            StartCoroutine(CoClose());
        }

        private IEnumerator CoClose()
        {
            if (_animator)
            {
                _animCloseEnd = false;
                _animator.Play("Close");
                yield return new WaitUntil(() => _animCloseEnd);
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

        public void AnimCloseEnd()
        {
            _animCloseEnd = true;
        }
    }
}
