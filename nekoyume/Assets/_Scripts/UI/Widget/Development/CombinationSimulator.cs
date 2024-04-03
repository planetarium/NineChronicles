using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CombinationSimulator : Widget
    {
        [SerializeField]
        private GameObject content;

        [SerializeField]
        private TMP_InputField inputField;

        public override WidgetType WidgetType => WidgetType.Development;

        private static readonly Cheat.DebugRandom _random = new Cheat.DebugRandom();

        private class Result
        {
            public readonly int subRecipeId;
            public readonly List<int> expects = new List<int>();
            public readonly List<int> results = new List<int>();

            public Result(int subRecipeId, int count)
            {
                this.subRecipeId = subRecipeId;
                for (var i = 0; i < count; i++)
                {
                    expects.Add(0);
                    results.Add(0);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            inputField.text = "1";
        }

        public void OnClickActive()
        {
            content.SetActive(!content.activeSelf);
        }

        public void OnClickCombinationSimulate()
        {
            AsyncCombinationSimulate();
        }

        public void OnClickEnhancementSimulate()
        {
            AsyncEnhancementSimulate(new Cheat.DebugRandom(_random.Next(1, 999999999)));
        }

        private async void AsyncCombinationSimulate()
        {
            var equipmentItemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            var equipmentReceipeSheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            var subRecipeSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2;
            var itemOptionSheet = Game.Game.instance.TableSheets.EquipmentItemOptionSheet;
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            var count = int.Parse(inputField.text);
            var results = new Dictionary<int, List<Result>>();

            NcDebug.Log($"-------------S T A R T (Combination) [COUNT] : {count}----------");
            var task = Task.Run(() =>
            {
                foreach (var recipe in equipmentReceipeSheet)
                {
                    if (equipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var row))
                    {
                        foreach (var subRecipeId in recipe.SubRecipeIds)
                        {
                            if (!subRecipeSheet.TryGetValue(subRecipeId, out var subRecipeRow))
                            {
                                continue;
                            }

                            for (var i = 0; i < count; ++i)
                            {
                                SetResult(row, subRecipeRow, itemOptionSheet, skillSheet, results,
                                    recipe, subRecipeId);
                            }
                        }

                        var result = results.Last();
                        foreach (var r in result.Value)
                        {
                            DrawResult(result.Key, count, r);
                        }
                    }
                }

                return true;
            });

            var finish = await task;
            if (finish)
            {
                NcDebug.Log("-------------F I N I S H (Combination)----------");
            }
        }

        private static void DrawResult(int itemId, int count, Result result)
        {
            var numbers = new List<int>();
            for (var i = 0; i < result.expects.Count; i++)
            {
                numbers.Add(i);
            }

            var results = numbers.Select((t, i) => GetExpectRatio(result, numbers, i + 1)).ToList();
            NcDebug.Log($"[CS] [{L10nManager.Localize($"ITEM_NAME_{itemId}")}] {itemId} / " +
                      $"[subRecipeId] {result.subRecipeId} / " +
                      $"<color=#5FD900>[1]</color><color=#0078FF>{results[0]:P2}</color> <color=#00A4FF> --> {(result.results[0] / (float)count):P2}</color> / " +
                      $"<color=#5FD900>[2]</color><color=#FF1800>{results[1]:P2}</color> <color=#F16558> --> {(result.results[1] / (float)count):P2}</color> / " +
                      $"<color=#5FD900>[3]</color><color=#0078FF>{results[2]:P2}</color> <color=#00A4FF> --> {(result.results[2] / (float)count):P2}</color> / " +
                      $"<color=#5FD900>[4]</color><color=#FF1800>{results[3]:P2}</color> <color=#F16558> --> {(result.results[3] / (float)count):P2}</color>");
        }

        private static decimal GetExpectRatio(Result result, IEnumerable<int> numbers, int count)
        {
            var combinations = numbers.DifferentCombinations(count);
            decimal sum = 0;
            foreach (var combination in combinations)
            {
                decimal value = 1;
                for (var i = 0; i < 4; i++)
                {
                    decimal ratio;
                    if (combination.ToList().Exists(x=> x == i))
                    {
                        ratio = (result.expects[i].NormalizeFromTenThousandths());
                    }
                    else
                    {
                        ratio = 1 - result.expects[i].NormalizeFromTenThousandths();
                    }

                    value *= ratio;
                }
                sum += value;
            }

            return sum;
        }

        private static void SetResult(EquipmentItemSheet.Row row,
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow,
            EquipmentItemOptionSheet itemOptionSheet,
            SkillSheet skillSheet,
            IDictionary<int, List<Result>> results,
            EquipmentItemRecipeSheet.Row recipe,
            int subRecipeId)
        {
            var equipment = CombinationEquipment(row, subRecipeRow, itemOptionSheet, skillSheet,
                new Cheat.DebugRandom(_random.Next(1, 999999999)));
            if (!results.ContainsKey(equipment.Id))
            {
                var resultList = recipe.SubRecipeIds
                    .Select(id => new Result(id, subRecipeRow.Options.Count)).ToList();
                results.Add(equipment.Id, resultList);
            }

            var item = results[equipment.Id].FirstOrDefault(x => x.subRecipeId == subRecipeId);

            var ratio = subRecipeRow.Options.Select(x => x.Ratio).ToList();
            for (var i = 0; i < ratio.Count; i++)
            {
                item.expects[i] = ratio[i];
            }

            item.results[equipment.optionCountFromCombination - 1] += 1;
        }

        private static Equipment CombinationEquipment(ItemSheet.Row row,
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow,
            EquipmentItemOptionSheet itemOptionSheet,
            SkillSheet skillSheet,
            IRandom random)
        {
            var equipment =
                (Equipment)ItemFactory.CreateItemUsable(row, random.GenerateRandomGuid(), 0);
            var agentState = States.Instance.AgentState;
            // Action.CombinationEquipment.AddAndUnlockOption(
            //     agentState,
            //     equipment,
            //     random,
            //     subRecipeRow,
            //     itemOptionSheet,
            //     skillSheet);
            return equipment;
        }

        private async void AsyncEnhancementSimulate(IRandom random)
        {
            var count = int.Parse(inputField.text);
            NcDebug.Log($"-------------S T A R T (Enhancement) [COUNT] : {count}----------");
            var task = Task.Run(() =>
            {
                var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
/*                foreach (var row in sheet)
                {
                    var counts = new Dictionary<ItemEnhancement.EnhancementResult, int>()
                    {
                        { ItemEnhancement.EnhancementResult.GreatSuccess, 0 },
                        { ItemEnhancement.EnhancementResult.Success, 0 },
                        { ItemEnhancement.EnhancementResult.Fail, 0 },
                    };
                    for (var i = 0; i < count; i++)
                    {
                        var equipmentResult = ItemEnhancement.GetEnhancementResult(row, random);
                        counts[equipmentResult] += 1;
                    }

                    Debug.Log($"[ES] [id] : {row.Id} / " +
                              $"<color=#00FF65>[GreatSuccess] {(row.GreatSuccessRatio.NormalizeFromTenThousandths()):P2}</color> --> <color=#8CFF00>{(counts[ItemEnhancement.EnhancementResult.GreatSuccess] / (float)count):P2}</color> / " +
                              $"<color=#0078FF>[Success] {(row.SuccessRatio.NormalizeFromTenThousandths()):P2}</color> --> <color=#00A4FF>{(counts[ItemEnhancement.EnhancementResult.Success] / (float)count):P2}</color> / " +
                              $"<color=#FF1800>[Fail] {(row.FailRatio.NormalizeFromTenThousandths()):P2}</color> --> <color=#F16558>{(counts[ItemEnhancement.EnhancementResult.Fail] / (float)count):P2}</color>");
                }
*/
                return true;
            });

            var finish = await task;
            if (finish)
            {
                NcDebug.Log("-------------F I N I S H (Enhancement)----------");
            }
        }
    }
}
