using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI.Module
{
    using System.Threading;
    using System.Threading.Tasks;
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

            SetOptionTag(model.ItemBase.Value);
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
                enhancementText.text = $"+{level}";
                enhancementText.enabled = true;
            }

            if (level >= Util.VisibleEnhancementEffectLevel)
            {
                var data = itemViewData.GetItemViewData(grade);
                enhancementImage.GetComponent<Image>().material = data.EnhancementMaterial;
                enhancementImage.SetActive(true);
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
