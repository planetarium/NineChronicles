using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Widget : MonoBehaviour
    {
        private struct PoolElementModel
        {
            public GameObject gameObject;
            public Widget widget;
        }

        private static readonly Dictionary<Type, PoolElementModel> Pool = new Dictionary<Type, PoolElementModel>();
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        private Material _glass;
        private bool _animCloseEnd;

        protected Animator Animator { get; set; }
        public RectTransform RectTransform { get; private set; }
        public virtual WidgetType WidgetType => WidgetType.Widget;

        protected virtual void Awake()
        {
            FindGlassMaterial(gameObject);

            Animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();
        }

        public virtual void Initialize()
        {
        }

        public static T Create<T>(bool activate = false) where T : Widget
        {
            var type = typeof(T);
            var names = type.ToString().Split('.');
            var resName = $"UI/Prefabs/UI_{names[names.Length - 1]}";
            var res = Resources.Load<GameObject>(resName);
            if (!ReferenceEquals(res, null))
            {
                var go = Instantiate(res, MainCanvas.instance.transform);
                var widget = go.GetComponent<T>();
                switch (widget.WidgetType)
                {
                    case WidgetType.Popup:
                    case WidgetType.Screen:
                    case WidgetType.Tooltip:
                    case WidgetType.Widget:
                    case WidgetType.SystemInfo:
                        if (Pool.ContainsKey(type))
                        {
                            Debug.LogWarning($"Duplicated create widget: {type}");
                            DestroyImmediate(go);
                            Pool[type].gameObject.SetActive(activate);

                            return (T) Pool[type].widget;
                        }

                        go.transform.SetParent(MainCanvas.instance.widget.transform);
                        go.SetActive(activate);
                        Pool.Add(type, new PoolElementModel
                        {
                            gameObject = go,
                            widget = widget
                        });
                        break;
                    case WidgetType.Hud:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                go.transform.SetParent(MainCanvas.instance.GetTransform(widget.WidgetType));
                go.SetActive(activate);

                return widget;
            }

            Debug.LogWarning(($"widget not exist: {type}"));

            return null;
        }

        public static T Find<T>() where T : Widget
        {
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
                throw new WidgetNotFoundException(type.Name);
            }

            return (T) model.widget;
        }


        private void FindGlassMaterial(GameObject go)
        {
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (!image || !image.material || image.material.shader.name != "UI/Unlit/FrostedGlass")
            {
                return;
            }

            _glass = image.material;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (Animator)
            {
                Animator.enabled = true;
                Animator.Play("Show");
            }

            StartCoroutine(Blur());
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

            var from = 0f;
            var to = _glass.GetFloat(Radius);

            _glass.SetFloat(Radius, from);
            var time = 0f;
            while (true)
            {
                var current = Mathf.Lerp(from, to, time);
                _glass.SetFloat(Radius, current);

                time += Time.deltaTime * 3f;
                if (time > 1f ||
                    !gameObject.activeInHierarchy)
                {
                    break;
                }

                yield return null;
            }

            _glass.SetFloat(Radius, to);
        }

        public virtual void Close()
        {
            StopAllCoroutines();
            if (!gameObject.activeSelf)
            {
                return;
            }

            // TODO : wait close animation
            StartCoroutine(CoClose());
        }

        public virtual IEnumerator CoClose()
        {
            if (Animator)
            {
                _animCloseEnd = false;
                Animator.enabled = true;
                Animator.Play("Close");
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

        public virtual void OnCompleteOfShowAnimation()
        {
            Animator.enabled = false;
        }

        public virtual void OnCompleteOfCloseAnimation()
        {
            _animCloseEnd = true;
        }
    }
}
