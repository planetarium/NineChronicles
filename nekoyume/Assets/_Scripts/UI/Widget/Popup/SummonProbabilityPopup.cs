using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SummonProbabilityPopup : PopupWidget
    {
        [SerializeField]
        private SummonDetailScroll scroll;

        [SerializeField]
        private TextMeshProUGUI titleText;

        private readonly List<IDisposable> _disposables = new();
        public void Show(SummonResult summonResult)
        {
            var summonRows = SummonFrontHelper.GetSummonRowsBySummonResult(summonResult);
            var tableSheets = Game.Game.instance.TableSheets;
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var runeSheet = tableSheets.RuneSheet;
            var equipmentItemSubRecipeSheet = tableSheets.EquipmentItemSubRecipeSheetV2;

            var modelDict = new Dictionary<int,SummonDetailCell.Model>();
            titleText.SetText(summonResult.ToString());
            foreach (var row in summonRows)
            {
                float ratioSum = row.Recipes.Sum(pair => pair.Item2);
                foreach (var (recipeId, ratio) in row.Recipes.Where(pair => pair.Item1 > 0))
                {
                    EquipmentItemSheet.Row equipmentRow = null;
                    List<EquipmentItemSubRecipeSheetV2.OptionInfo> equipmentOptions = null;
                    if (equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
                    {
                        equipmentRow = equipmentItemSheet[recipeRow.ResultEquipmentId];
                        equipmentOptions = equipmentItemSubRecipeSheet[recipeRow.SubRecipeIds[0]]
                            .Options;
                    }

                    string runeTicker = null;
                    RuneOptionSheet.Row.RuneOptionInfo runeOptionInfo = null;
                    if (runeSheet.TryGetValue(recipeId, out var runeRow))
                    {
                        runeTicker = runeRow.Ticker;

                        var runeOptionSheet = tableSheets.RuneOptionSheet;
                        if (runeOptionSheet.TryGetValue(runeRow.Id, out var runeOptionRow))
                        {
                            runeOptionRow.LevelOptionMap.TryGetValue(1, out runeOptionInfo);
                        }
                    }

                    if (modelDict.TryGetValue(recipeId, out var model))
                    {
                        var cellRatio = ratio / ratioSum;
                        switch ((CostType) row.CostMaterial)
                        {
                            case CostType.GoldDust:
                                model.GoldRatio = cellRatio;
                                break;
                            case CostType.RubyDust:
                                model.RubyRatio = cellRatio;
                                break;
                            case CostType.EmeraldDust:
                                model.EmeraldRatio = cellRatio;
                                break;
                            case CostType.SilverDust:
                                model.SilverRatio = cellRatio;
                                break;
                        }
                    }
                    else
                    {
                        var cellRatio = ratio / ratioSum;
                        var grade = equipmentRow?.Grade ?? Util.GetTickerGrade(runeTicker);
                        var cellModel = new SummonDetailCell.Model
                        {
                            EquipmentRow = equipmentRow,
                            EquipmentOptions = equipmentOptions,
                            RuneTicker = runeTicker,
                            RuneOptionInfo = runeOptionInfo,
                            Grade = grade,
                        };
                        switch ((CostType) row.CostMaterial)
                        {
                            case CostType.GoldDust:
                                cellModel.GoldRatio = cellRatio;
                                break;
                            case CostType.RubyDust:
                                cellModel.RubyRatio = cellRatio;
                                break;
                            case CostType.EmeraldDust:
                                cellModel.EmeraldRatio = cellRatio;
                                break;
                            case CostType.SilverDust:
                                cellModel.SilverRatio = cellRatio;
                                break;
                        }

                        modelDict.Add(recipeId, cellModel);
                    }
                }
            }

            _disposables.DisposeAllAndClear();
            scroll.UpdateData(modelDict.Values.OrderByDescending(model => model.Grade), true);

            //titleText.text = rowList.FirstOrDefault().GetLocalizedName();
            base.Show();
        }
    }
}
