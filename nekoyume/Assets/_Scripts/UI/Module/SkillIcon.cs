using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SkillIcon : MonoBehaviour
    {
        [SerializeField]
        private Image skillIcon;

        [SerializeField]
        private TouchHandler touchHandler;

        private readonly List<IDisposable> _disposables = new();
        private System.Action _callback;

        private void Awake()
        {
            touchHandler.OnClick
                .Subscribe(_ =>
                {
                    _callback?.Invoke();
                })
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        public void Set(Sprite icon, System.Action callback)
        {
            skillIcon.sprite = icon;
            skillIcon.SetNativeSize();
            _callback = callback;
        }
    }
}
