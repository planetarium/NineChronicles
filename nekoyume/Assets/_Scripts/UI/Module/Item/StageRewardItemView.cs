using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class StageRewardItemView : VanillaItemView
    {
        public RectTransform RectTransform { get; private set; }
        public ItemBase Data { get; private set; }

        public TouchHandler touchHandler;

        private readonly List<IDisposable> _disposables = new();

        protected override ImageSizeType imageSizeType => ImageSizeType.Small;

        protected void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        protected void OnDestroy()
        {
            Clear();
        }

        public override void SetData(ItemBase itemBase, System.Action onClick = null)
        {
            base.SetData(itemBase, onClick);
            Data = itemBase;
            _disposables.DisposeAllAndClear();
            if (touchHandler != null && onClick != null)
            {
                touchHandler.OnClick.Subscribe(_ => onClick?.Invoke()).AddTo(_disposables);
            }
        }
    }
}
