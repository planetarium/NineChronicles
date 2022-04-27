using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(BaseItemView))]
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(InventoryItem model, InventoryScroll.ContextModel context)
        {
            if (model is null)
            {
                baseItemView.Container.SetActive(false);
                baseItemView.EmptyObject.SetActive(true);
                return;
            }

            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = baseItemView.GetItemIcon(model.ItemBase);

            var data = baseItemView.GetItemViewData(model.ItemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (model.ItemBase is Equipment equipment && equipment.level > 0)
            {
                baseItemView.EnhancementText.gameObject.SetActive(true);
                baseItemView.EnhancementText.text = $"+{equipment.level}";
                if (equipment.level >= Util.VisibleEnhancementEffectLevel)
                {
                    baseItemView.EnhancementImage.material = data.EnhancementMaterial;
                    baseItemView.EnhancementImage.gameObject.SetActive(true);
                }
                else
                {
                    baseItemView.EnhancementImage.gameObject.SetActive(false);
                }
            }
            else
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
            }

            baseItemView.OptionTag.Set(model.ItemBase);

            baseItemView.CountText.gameObject.SetActive(
                model.ItemBase.ItemType == ItemType.Material);
            baseItemView.CountText.text = model.Count.Value.ToString();

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b)).AddTo(_disposables);
            model.LevelLimited.Subscribe(b => baseItemView.LevelLimitObject.SetActive(b)).AddTo(_disposables);
            model.DimObjectEnabled.Subscribe(b => baseItemView.DimObject.SetActive(b)).AddTo(_disposables);
            model.Tradable.Subscribe(b => baseItemView.TradableObject.SetActive(b)).AddTo(_disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b)).AddTo(_disposables);
            model.Focused.Subscribe(b => baseItemView.FocusObject.SetActive(b)).AddTo(_disposables);
            model.HasNotification.Subscribe(b => baseItemView.NotificationObject.SetActive(b)).AddTo(_disposables);
            model.GrindingCount.Subscribe(count =>
            {
                baseItemView.GrindingCountObject.SetActive(count > 0);
                if (count > 0)
                {
                    baseItemView.GrindingCountText.text = count.ToString();
                }
            }).AddTo(_disposables);
            model.GrindingCountEnabled
                .Subscribe(b => baseItemView.GrindingCountObject.SetActive(b))
                .AddTo(_disposables);
            model.View = GetComponent<RectTransform>();

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(_disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(_disposables);
        }
    }
}
