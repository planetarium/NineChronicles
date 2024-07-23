using Nekoyume.Helper;
using Nekoyume.L10n;
using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class SummonDetailCell : RectCell<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class Model
        {
            public EquipmentItemSheet.Row EquipmentRow;
            public List<EquipmentItemSubRecipeSheetV2.OptionInfo> EquipmentOptions;
            public string RuneTicker;
            public RuneOptionSheet.Row.RuneOptionInfo RuneOptionInfo;
            public float Ratio;
        }

        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private GameObject selected;

        private readonly List<IDisposable> _disposables = new();

        public override void UpdateContent(Model itemData)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Context?.OnClick.OnNext(itemData);
            });

            if (itemData.EquipmentRow is not null)
            {
                iconImage.sprite = SpriteHelper.GetItemIcon(itemData.EquipmentRow.Id);
                nameText.text = itemData.EquipmentRow.GetLocalizedName(true, false);
                infoText.text = itemData.EquipmentRow.GetLocalizedInformation();
            }

            if (!string.IsNullOrEmpty(itemData.RuneTicker))
            {
                iconImage.sprite = SpriteHelper.GetFavIcon(itemData.RuneTicker);
                nameText.text = LocalizationExtensions.GetLocalizedFavName(itemData.RuneTicker);
                infoText.text = LocalizationExtensions.GetLocalizedInformation(itemData.RuneTicker);
            }

            percentText.text = itemData.Ratio.ToString("0.####%");

            _disposables.DisposeAllAndClear();
            Context?.Selected.Subscribe(model =>
                selected.SetActive(itemData.Equals(model))).AddTo(_disposables);
        }
    }
}
