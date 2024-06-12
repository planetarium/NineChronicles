using System;
using System.Collections.Generic;
using System.Linq;
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
        protected BaseItemView baseItemView;

        protected readonly List<IDisposable> Disposables = new List<IDisposable>();

        public void Set(InventoryItem model, InventoryScroll.ContextModel context)
        {
            if (model is null)
            {
                baseItemView.Container.SetActive(false);
                baseItemView.EmptyObject.SetActive(true);
                return;
            }

            if (model.ItemBase != null)
            {
                UpdateItem(model, context);
            }
            else if (model.RuneState != null)
            {
                UpdateRune(model, context);
            }
            else
            {
                UpdateFungibleAsset(model, context);
            }
        }

        protected virtual void UpdateItem(InventoryItem model, InventoryScroll.ContextModel context)
        {
            Disposables.DisposeAllAndClear();
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
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

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

            baseItemView.OptionTag.gameObject.SetActive(true);
            baseItemView.OptionTag.Set(model.ItemBase);

            if (model.ItemBase.ItemType == ItemType.Consumable)
            {
                baseItemView.CountText.gameObject.SetActive(true);
                model.Count.Subscribe(value => baseItemView.CountText.text = value.ToString())
                    .AddTo(Disposables);
            }
            else
            {
                baseItemView.CountText.gameObject.SetActive(model.Count.Value > 1);
                baseItemView.CountText.text = model.Count.Value.ToString();
            }

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b))
                .AddTo(Disposables);
            model.LevelLimited.Subscribe(b => baseItemView.LevelLimitObject.SetActive(b))
                .AddTo(Disposables);
            model.DimObjectEnabled.Subscribe(b => baseItemView.DimObject.SetActive(b))
                .AddTo(Disposables);
            model.Tradable.Subscribe(b => baseItemView.TradableObject.SetActive(b))
                .AddTo(Disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b))
                .AddTo(Disposables);
            model.Focused.Subscribe(b => baseItemView.FocusObject.SetActive(b)).AddTo(Disposables);
            model.HasNotification.Subscribe(b => baseItemView.NotificationObject.SetActive(b))
                .AddTo(Disposables);
            model.GrindingCountEnabled
                .Subscribe(b => baseItemView.GrindingCountObject.SetActive(b))
                .AddTo(Disposables);
            model.CollectionSelected
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b))
                .AddTo(Disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(Disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(Disposables);
        }

        protected virtual void UpdateRune(InventoryItem model, InventoryScroll.ContextModel context)
        {
            Disposables.DisposeAllAndClear();
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
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

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

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b))
                .AddTo(Disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b))
                .AddTo(Disposables);
            model.Focused.Subscribe(b => baseItemView.FocusObject.SetActive(b)).AddTo(Disposables);
            model.DimObjectEnabled.Subscribe(b => baseItemView.DimObject.SetActive(b))
                .AddTo(Disposables);
            model.HasNotification.Subscribe(b => baseItemView.NotificationObject.SetActive(b))
                .AddTo(Disposables);
            model.CollectionSelected
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b))
                .AddTo(Disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(Disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(Disposables);
        }

        protected virtual void UpdateFungibleAsset(InventoryItem model, InventoryScroll.ContextModel context)
        {
            Disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.EnhancementText.gameObject.SetActive(false);
            baseItemView.EnhancementImage.gameObject.SetActive(false);
            baseItemView.OptionTag.gameObject.SetActive(false);
            baseItemView.CountText.gameObject.SetActive(true);
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            baseItemView.CountText.text = model.FungibleAssetValue.GetQuantityString();
            baseItemView.ItemImage.overrideSprite = model.FungibleAssetValue.GetIconSprite();

            var ticker = model.FungibleAssetValue.Currency.Ticker;

            var grade = 1;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                grade = petRow.Grade;
            }

            var data = baseItemView.GetItemViewData(grade);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            model.Equipped.Subscribe(b => baseItemView.EquippedObject.SetActive(b))
                .AddTo(Disposables);
            model.Selected.Subscribe(b => baseItemView.SelectObject.SetActive(b))
                .AddTo(Disposables);
            model.Focused.Subscribe(b => baseItemView.FocusObject.SetActive(b)).AddTo(Disposables);
            model.DimObjectEnabled.Subscribe(b => baseItemView.DimObject.SetActive(b))
                .AddTo(Disposables);
            model.HasNotification.Subscribe(b => baseItemView.NotificationObject.SetActive(b))
                .AddTo(Disposables);
            model.CollectionSelected
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b))
                .AddTo(Disposables);

            baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(Disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(Disposables);
        }
    }
}
