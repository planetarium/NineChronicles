using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class RuneStoneEnhancementInventoryCell : GridCell<RuneStoneEnhancementInventoryItem, RuneStoneEnhancementInventoryScroll.ContextModel>
    {
        [SerializeField]
        private BaseItemView view;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        public override void UpdateContent(RuneStoneEnhancementInventoryItem viewModel)
        {
            _disposables.DisposeAllAndClear();

            if (viewModel is null)
            {
                view.Container.SetActive(false);
                view.EmptyObject.SetActive(true);
                return;
            }
            //view.Set(viewModel, Context);
            view.Container.SetActive(true);
            view.EmptyObject.SetActive(false);
            view.EnoughObject.SetActive(false);
            view.MinusObject.SetActive(false);
            view.ExpiredObject.SetActive(false);
            view.SelectBaseItemObject.SetActive(false);
            view.SelectMaterialItemObject.SetActive(false);
            view.LockObject.SetActive(false);
            view.ShadowObject.SetActive(false);
            view.PriceText.gameObject.SetActive(false);
            view.LoadingObject.SetActive(false);
            view.CountText.gameObject.SetActive(false);
            view.LevelLimitObject.SetActive(false);
            view.TradableObject.SetActive(false);
            view.GrindingCountObject.SetActive(false);
            view.RuneNotificationObj.SetActiveSafe(false);
            view.RuneSelectMove.SetActive(false);
            view.SelectCollectionObject.SetActive(false);
            view.SelectArrowObject.SetActive(false);

            if (RuneFrontHelper.TryGetRuneIcon(viewModel.SheetData.Id, out var icon))
            {
                view.ItemImage.overrideSprite = icon;
            }

            var data = view.GetItemViewData(viewModel.SheetData.Grade);
            view.GradeImage.overrideSprite = data.GradeBackground;
            view.GradeHsv.range = data.GradeHsvRange;
            view.GradeHsv.hue = data.GradeHsvHue;
            view.GradeHsv.saturation = data.GradeHsvSaturation;
            view.GradeHsv.value = data.GradeHsvValue;

            if(viewModel.State is null)
            {
                view.EnhancementText.gameObject.SetActive(false);
                view.EnhancementImage.gameObject.SetActive(false);
            }
            else
            {
                view.EnhancementText.gameObject.SetActive(true);
                view.EnhancementText.text = $"+{viewModel.State.Level}";
                view.EnhancementImage.gameObject.SetActive(false);
            }

            view.EquippedObject.SetActive(false);
            view.SelectObject.SetActive(false);
            view.FocusObject.SetActive(false);
            view.FocusObject.SetActive(false);
            view.NotificationObject.SetActive(false);
            view.DimObject.SetActive(viewModel.State is null);

            view.OptionTag.gameObject.SetActive(false);

            view.TouchHandler.OnClick.Select(_ => viewModel)
                .Subscribe(Context.OnClick.OnNext)
                .AddTo(_disposables);

            view.RuneNotificationObj.SetActiveSafe(viewModel.item.HasNotification);

            viewModel.item.IsSelected.Subscribe(b => view.RuneSelectMove.SetActive(b)).AddTo(_disposables);
            viewModel.item.IsSelected.Value = false;

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(viewModel.SheetData.Id, out var optionRow))
            {
                return;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(viewModel.State is null ? 0: viewModel.State.Level, out var option))
            {
                return;
            }

            view.OptionTag.gameObject.SetActive(option.SkillId != 0);
            if (option.SkillId != 0)
            {
                view.OptionTag.Set(viewModel.SheetData.Grade);
            }


            /*baseItemView.TouchHandler.OnClick.Select(_ => model)
                .Subscribe(context.OnClick.OnNext).AddTo(_disposables);
            baseItemView.TouchHandler.OnDoubleClick.Select(_ => model)
                .Subscribe(context.OnDoubleClick.OnNext).AddTo(_disposables);*/
        }
    }
}
