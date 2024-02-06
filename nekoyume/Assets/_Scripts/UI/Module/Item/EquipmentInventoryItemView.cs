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
    public class EquipmentInventoryItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(EnhancementInventoryItem model, EnhancementInventoryScroll.ContextModel context)
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

            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.CountText.gameObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = BaseItemView.GetItemIcon(model.ItemBase);

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

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b)).AddTo(_disposables);
            model.LevelLimited.Subscribe(b => baseItemView.LevelLimitObject.SetActive(b)).AddTo(_disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b)).AddTo(_disposables);
            model.SelectedBase.Subscribe(b => baseItemView.SelectBaseItemObject.SetActive(b)).AddTo(_disposables);
            model.SelectedMaterial.Subscribe(b => baseItemView.SelectMaterialItemObject.SetActive(b)).AddTo(_disposables);
            model.Disabled.Subscribe(b => baseItemView.DimObject.SetActive(b)).AddTo(_disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(_disposables);

            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(_disposables);
        }
    }
}
