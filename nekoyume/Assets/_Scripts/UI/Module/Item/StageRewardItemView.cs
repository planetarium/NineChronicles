using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class StageRewardItemView: VanillaItemView
    {
        public RectTransform RectTransform { get; private set; }
        public ItemSheet.Row Data { get; private set; }

        public TouchHandler touchHandler;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected override ImageSizeType imageSizeType => ImageSizeType.Small;

        protected void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        protected void OnDestroy()
        {
            Clear();
        }

        public override void SetData(ItemSheet.Row itemRow, System.Action onClick = null)
        {
            base.SetData(itemRow, onClick);
            Data = itemRow;
            _disposables.DisposeAllAndClear();
            if (touchHandler != null && onClick != null)
            {
                touchHandler.OnClick.Subscribe(_ => onClick?.Invoke()).AddTo(_disposables);
            }
        }
    }
}
