using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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

            if (model.RuneState != null)
            {
                UpdateRune(model, context);
            }
            else
            {
                UpdateItem(model, context);
            }
        }

        private void UpdateRune(InventoryItem model, InventoryScroll.ContextModel context)
        {
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
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.CountText.gameObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);

            if (RuneFrontHelper.TryGetRuneIcon(model.RuneState.RuneId, out var icon))
            {
                baseItemView.ItemImage.overrideSprite = icon;
            }

            var sheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (sheet.TryGetValue(model.RuneState.RuneId, out var row))
            {
                var data = baseItemView.GetItemViewData(row.Grade);
                baseItemView.GradeImage.overrideSprite = data.GradeBackground;
                baseItemView.GradeHsv.range = data.GradeHsvRange;
                baseItemView.GradeHsv.hue = data.GradeHsvHue;
                baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
                baseItemView.GradeHsv.value = data.GradeHsvValue;
            }

            baseItemView.EnhancementText.gameObject.SetActive(true);
            baseItemView.EnhancementText.text = $"+{model.RuneState.Level}";
            baseItemView.EnhancementImage.gameObject.SetActive(false);

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(row.Id, out var optionRow))
            {
                return;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(model.RuneState.Level, out var option))
            {
                return;
            }

            baseItemView.OptionTag.gameObject.SetActive(option.SkillId != 0);
            if (option.SkillId != 0)
            {
                baseItemView.OptionTag.Set(row.Grade);
            }

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b)).AddTo(_disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b)).AddTo(_disposables);
            model.Focused.Subscribe(b => baseItemView.FocusObject.SetActive(b)).AddTo(_disposables);
            model.DimObjectEnabled.Subscribe(b => baseItemView.DimObject.SetActive(b)).AddTo(_disposables);
            model.HasNotification.Subscribe(b => baseItemView.NotificationObject.SetActive(b)).AddTo(_disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(_disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(_disposables);
        }

        private void UpdateItem(InventoryItem model, InventoryScroll.ContextModel context)
        {
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
            baseItemView.LoadingObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite =
                BaseItemView.GetItemIcon(model.ItemBase);

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

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(_disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(_disposables);
        }
    }
}
