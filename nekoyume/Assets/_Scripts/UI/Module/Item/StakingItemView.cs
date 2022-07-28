using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;
    [RequireComponent(typeof(BaseItemView))]
    public class StakingItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(ItemBase itemBase, BigInteger count, Action<ItemBase> onClick)
        {
            if (itemBase == null)
            {
                return;
            }

            Set(BaseItemView.GetItemIcon(itemBase),
                count.ToString(),
                () => onClick(itemBase));
        }

        public void Set(Sprite sprite, string description, System.Action onClick)
        {
            baseItemView.ItemImage.sprite = sprite;
            Set(description, onClick);
        }

        public void Set(string description, System.Action onClick)
        {
            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.TouchHandler.gameObject.SetActive(true);
            baseItemView.CountText.text = description;

            baseItemView.TouchHandler.OnClick
                .Subscribe(_ => onClick())
                .AddTo(_disposables);
        }
    }
}
