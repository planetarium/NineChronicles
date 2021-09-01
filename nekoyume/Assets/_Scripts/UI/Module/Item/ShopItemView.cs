using System;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ShopItemView : CountableItemView<ShopItem>
    {
        public GameObject priceGroup;
        public TextMeshProUGUI priceText;
        [SerializeField] private GameObject expired;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _expiredBlockIndex;

        public override void SetData(ShopItem model)
        {
            if (model is null)
            {
                Clear();
                return;
            }

            base.SetData(model);
            SetBg(1f);
            SetLevel(model.ItemBase.Value.Grade, model.Level.Value);
            priceGroup.SetActive(true);
            priceText.text = model.Price.Value.GetQuantityString();
            Model.View = this;

            if (expired)
            {
                _expiredBlockIndex = model.ExpiredBlockIndex.Value;
                SetExpired(Game.Game.instance.Agent.BlockIndex);
                Game.Game.instance.Agent.BlockIndexSubject
                    .Subscribe(SetExpired)
                    .AddTo(_disposables);
            }
        }

        public override void Clear()
        {
            if (Model != null)
            {
                Model.Selected.Value = false;
            }

            base.Clear();

            SetBg(0f);
            SetLevel(0, 0);
            priceGroup.SetActive(false);
            if (expired != null)
            {
                expired.SetActive(false);
            }
            _disposables.DisposeAllAndClear();
        }

        private void SetBg(float alpha)
        {
            var a = alpha;
            var color = backgroundImage.color;
            color.a = a;
            backgroundImage.color = color;
        }

        private void SetLevel(int grade, int level)
        {
            if (level > 0)
            {
                var data = itemViewData.GetItemViewData(grade);
                enhancementImage.GetComponent<Image>().material = data.EnhancementMaterial;
                enhancementImage.SetActive(true);
                enhancementText.text = $"+{level}";
                enhancementText.enabled = true;
            }
        }

        private void SetExpired(long blockIndex)
        {
            if (expired)
            {
                expired.SetActive(_expiredBlockIndex - blockIndex <= 0);
            }
        }
    }
}
