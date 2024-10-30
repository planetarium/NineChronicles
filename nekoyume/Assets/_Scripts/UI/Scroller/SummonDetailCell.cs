using Nekoyume.Helper;
using Nekoyume.L10n;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
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
            public float SilverRatio;
            public float GoldRatio;
            public float RubyRatio;
            public float EmeraldRatio;
            public int Grade;
        }

        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private TextMeshProUGUI silverPercentText;
        [SerializeField] private TextMeshProUGUI goldPercentText;
        [SerializeField] private TextMeshProUGUI rubyPercentText;
        [SerializeField] private TextMeshProUGUI emeraldPercentText;
        [SerializeField] private GameObject[] statObjects;
        [SerializeField] private TextMeshProUGUI[] statTexts;
        [SerializeField] private GameObject skillObject;
        [SerializeField] private TextMeshProUGUI skillText;

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
            skillObject.SetActive(false);
            skillText.gameObject.SetActive(false);
            foreach (var obj in statObjects)
            {
                obj.SetActive(false);
            }

            if (itemData.EquipmentRow is not null)
            {
                iconImage.sprite = SpriteHelper.GetItemIcon(itemData.EquipmentRow.Id);
                nameText.text = itemData.EquipmentRow.GetLocalizedName(true, false);
                infoText.text = itemData.EquipmentRow.GetLocalizedInformation();
                var statIndex = 0;
                foreach (var optionRow in itemData.EquipmentOptions.Select(info => TableSheets.Instance.EquipmentItemOptionSheet[info.Id]))
                {
                    if (optionRow.StatType != StatType.NONE)
                    {
                        statObjects[statIndex].SetActive(true);
                        statTexts[statIndex].SetText(optionRow.StatType.ToString());
                        statIndex++;
                    }
                    else
                    {
                        if (TableSheets.Instance.SkillSheet.TryGetValue(optionRow.SkillId,
                            out var skillRow))
                        {
                            skillObject.SetActive(true);
                            skillText.gameObject.SetActive(true);
                            skillText.SetText(skillRow.GetLocalizedName());
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(itemData.RuneTicker))
            {
                iconImage.sprite = SpriteHelper.GetFavIcon(itemData.RuneTicker);
                nameText.text = LocalizationExtensions.GetLocalizedFavName(itemData.RuneTicker);
                infoText.text = LocalizationExtensions.GetLocalizedInformation(itemData.RuneTicker);
                var statIndex = 0;
                foreach (var (stat, _) in itemData.RuneOptionInfo.Stats)
                {
                    if (stat.StatType != StatType.NONE)
                    {
                        statObjects[statIndex].SetActive(true);
                        statTexts[statIndex].SetText(stat.StatType.ToString());
                        statIndex++;
                    }
                }

                if (itemData.RuneOptionInfo.SkillId != 0)
                {
                    if (TableSheets.Instance.SkillSheet.TryGetValue(itemData.RuneOptionInfo.SkillId,
                        out var skillRow))
                    {
                        skillObject.SetActive(true);
                        skillText.gameObject.SetActive(true);
                        skillText.SetText(skillRow.GetLocalizedName());
                    }
                }
            }

            silverPercentText.text = GetRatioString(itemData.SilverRatio);
            goldPercentText.text = GetRatioString(itemData.GoldRatio);
            rubyPercentText.text = GetRatioString(itemData.RubyRatio);
            emeraldPercentText.text = GetRatioString(itemData.EmeraldRatio);
            if (itemData.Ratio != 0)
            {
                percentText.text = itemData.Ratio.ToString("0.####%");
            }

            _disposables.DisposeAllAndClear();
            Context?.Selected.Subscribe(model =>
                selected.SetActive(itemData.Equals(model))).AddTo(_disposables);
        }

        private static string GetRatioString(float ratio)
        {
            if (ratio == 0)
            {
                return "-";
            }

            return ratio.ToString("0.####%");
        }
    }
}
