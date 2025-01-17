using Nekoyume.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class SummonDetailCell : RectCell<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class Model
        {
            public EquipmentItemSheet.Row EquipmentRow;
            public CostumeItemSheet.Row CostumeRow;
            public List<CostumeStatSheet.Row> CostumeStatRows;
            public List<EquipmentItemSubRecipeSheetV2.OptionInfo> EquipmentOptions;
            public string RuneTicker;
            public RuneOptionSheet.Row.RuneOptionInfo RuneOptionInfo;
            public float Ratio;
            public readonly Dictionary<CostType, float> RatioByCostDict = new();
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

        private static readonly List<CostType> DustTypes = new()
            {CostType.SilverDust, CostType.GoldDust, CostType.RubyDust, CostType.EmeraldDust};

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
            foreach (var cost in DustTypes)
            {
                var textObj = GetTextByCostType(cost);
                textObj.gameObject.SetActive(Context.ContainedCost.Contains(cost));
                textObj.SetText(GetRatioString(0f));
            }

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
                    else if (TableSheets.Instance.SkillSheet.TryGetValue(optionRow.SkillId,
                        out var skillRow))
                    {
                        skillObject.SetActive(true);
                        skillText.gameObject.SetActive(true);
                        skillText.SetText(skillRow.GetLocalizedName());
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

                if (TableSheets.Instance.SkillSheet.TryGetValue(itemData.RuneOptionInfo.SkillId,
                    out var skillRow))
                {
                    skillObject.SetActive(true);
                    skillText.gameObject.SetActive(true);
                    skillText.SetText(skillRow.GetLocalizedName());
                }
            }

            if (itemData.CostumeRow is not null)
            {
                iconImage.sprite = SpriteHelper.GetItemIcon(itemData.CostumeRow.Id);
                nameText.text = itemData.CostumeRow.GetLocalizedName();
                var statIndex = 0;
                foreach (var statRow in itemData.CostumeStatRows)
                {
                    statObjects[statIndex].SetActive(true);
                    statTexts[statIndex].SetText(statRow.StatType.ToString());
                    statIndex++;
                }
            }

            foreach (var pair in itemData.RatioByCostDict)
            {
                var textObj = GetTextByCostType(pair.Key);
                textObj.SetText(GetRatioString(pair.Value));
            }

            if (itemData.Ratio != 0)
            {
                percentText.text = itemData.Ratio.ToString("0.####%");
            }

            _disposables.DisposeAllAndClear();
        }

        private static string GetRatioString(float ratio)
        {
            return ratio == 0 ? "-" : ratio.ToString("0.####%");
        }

        private TextMeshProUGUI GetTextByCostType(CostType costType)
        {
            return costType switch
            {
                CostType.SilverDust => silverPercentText,
                CostType.GoldDust => goldPercentText,
                CostType.RubyDust => rubyPercentText,
                CostType.EmeraldDust => emeraldPercentText,
            };
        }
    }
}
