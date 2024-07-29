using System;
using System.Collections.Generic;
using Nekoyume.Editor;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class CustomOutfitView : VanillaItemView
    {
        [SerializeField]
        private bool handleTouchEvent = true;

#if UNITY_EDITOR
        [ShowOn(nameof(handleTouchEvent))]
#endif
        public TouchHandler touchHandler;

        [SerializeField]
        private GameObject selection;

        [SerializeField]
        private GameObject disable;

        public readonly Subject<CustomOutfitView> OnClick = new();

        public List<IDisposable> DisposablesAtSetData { get; } = new();

        public CustomOutfit Model { get; private set; }

        public void SetData(CustomOutfit model)
        {
            Model = model;
            Model.Dimmed.SubscribeTo(disable).AddTo(DisposablesAtSetData);
            Model.Selected.SubscribeTo(selection).AddTo(DisposablesAtSetData);

            iconImage.enabled = true;
            iconImage.overrideSprite = SpriteHelper.GetItemIcon(Model.IconRow.Value.IconId);
            iconImage.SetNativeSize();
        }

        protected virtual void Awake()
        {
            if (handleTouchEvent)
            {
                touchHandler.OnClick.Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    OnClick.OnNext(this);
                    Model?.OnClick.OnNext(Model);
                }).AddTo(gameObject);
            }

            selection.transform.SetAsLastSibling();
            disable.transform.SetAsLastSibling();
        }

        protected virtual void OnDestroy()
        {
            Model?.Dispose();
            OnClick.Dispose();
        }
    }
}
