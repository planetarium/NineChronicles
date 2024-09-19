using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nekoyume.UI
{
    public class CraftResultPopup : PopupWidget
    {
        private struct MinMaxValue
        {
            public long Min;
            public long Max;
        }

        [SerializeField]
        private VanillaItemView itemView;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI baseStatText;

        [SerializeField]
        private List<CombinationResultOptionView> optionViews;

        [SerializeField]
        private CombinationResultOptionView skillView;

        [Header("Effects")]
        [SerializeField]
        private GameObject itemViewEffect;
        
        [SerializeField]
        private List<GameObject> viewEffects;

        public void Show(Equipment resultEquipment, int subRecipeId)
        {
            HideAllEffects();
            
            itemView.SetData(resultEquipment);
            itemNameText.SetText(resultEquipment.GetLocalizedName());
            baseStatText.SetText($"{resultEquipment.Stat.StatType} {resultEquipment.Stat.BaseValueAsLong}");
            optionViews.ForEach(view => view.Hide(true));
            skillView.Hide(true);

            var itemOptionInfo = new ItemOptionInfo(resultEquipment);
            var statOptions = GatherStatOptionsFromType(itemOptionInfo);
            var statOptionsCount = statOptions.Count;
            var skillOptions = itemOptionInfo.SkillOptions;
            var skillOptionsCount = skillOptions.Count;
            
            for (var i = 0; i < optionViews.Count; i++)
            {
                var optionView = optionViews[i];
                if (i >= statOptionsCount)
                {
                    optionView.UpdateToEmpty();
                    continue;
                }

                var (type, value, _) = statOptions[i];
                optionView.UpdateAsStatWithCount(type, value, GetStatRating(type, value, subRecipeId));
            }

            if (skillOptionsCount == 0)
            {
                skillView.UpdateToEmpty();
            }
            else
            {
                var (skillRow, power, chance, ratio, type) = skillOptions[0];
                var powerText = SkillExtensions.EffectToString(skillRow.Id, skillRow.SkillType, power, ratio, type);
                skillView.UpdateAsSkill(skillRow.GetLocalizedName(), powerText, chance, GetSkillRating(skillRow.Id, power, ratio, subRecipeId));
            }
            base.Show();
        }

        private List<(StatType type, long value, int count)> GatherStatOptionsFromType(ItemOptionInfo itemOptionInfo)
        {
            var statOptions = itemOptionInfo.StatOptions;
            var result = new Dictionary<StatType, (long value, int count)>();
            
            foreach (var (type, value, count) in statOptions)
            {
                if (result.ContainsKey(type))
                {
                    var (v, c) = result[type];
                    result[type] = (v + value, c + count);
                }
                else
                {
                    result.Add(type, (value, count));
                }
            }

            return result.Select(kv => (kv.Key, kv.Value.value, kv.Value.count)).ToList();
        }
        
        private readonly Dictionary<StatType, MinMaxValue> _statValues = new();
        private int GetStatRating(StatType type, long value, int subRecipeId)
        {
            _statValues.Clear();
            
            var equipmentItemSubRecipeSheet = TableSheets.Instance.EquipmentItemSubRecipeSheetV2;
            var row = equipmentItemSubRecipeSheet[subRecipeId];
            row.Options.ForEach(option =>
            {
                var optionRow = TableSheets.Instance.EquipmentItemOptionSheet[option.Id];
                var statType = optionRow.StatType;
                if (statType == StatType.NONE)
                {
                    return;
                }
                
                if (_statValues.ContainsKey(statType))
                {
                    var minMaxValue = new MinMaxValue
                    {
                        // 옵션은 확률적으로 부여되므로, 같은 스탯 타입의 옵션이 여러개인 경우 가장 낮은 값으로 설정
                        Min = Math.Min(_statValues[statType].Min, optionRow.StatMin),
                        Max = _statValues[statType].Max + optionRow.StatMax
                    };
                    _statValues[statType] = minMaxValue;
                }
                else
                {
                    _statValues.TryAdd(statType, new MinMaxValue
                    {
                        Min = optionRow.StatMin,
                        Max = optionRow.StatMax
                    });
                }
            });
            
            var minMaxValue = _statValues[type];
            var valueSubMin = value - minMaxValue.Min;
            var rating = 1 - valueSubMin / (double)(minMaxValue.Max - minMaxValue.Min);
            var ratingPercent = (int)(rating * 100);
            
            return ratingPercent;
        }

        private int GetSkillRating(int skillId, long power, int damageRatio, int subRecipeId)
        {
            var equipmentItemSubRecipeSheet = TableSheets.Instance.EquipmentItemSubRecipeSheetV2;
            var row = equipmentItemSubRecipeSheet[subRecipeId];
            
            var ratingPercent = 0;
            row.Options.ForEach(option =>
            {
                var optionRow = TableSheets.Instance.EquipmentItemOptionSheet[option.Id];
                var statType = optionRow.StatType;
                if (statType != StatType.NONE || optionRow.SkillId != skillId) 
                {
                    return;
                }

                if (damageRatio > 0 && optionRow.StatDamageRatioMin > 0)
                {
                    var valueSubMin = damageRatio - optionRow.StatDamageRatioMin;
                    var rating = 1 - valueSubMin / (double)(optionRow.StatDamageRatioMax - optionRow.StatDamageRatioMin);
                    ratingPercent = (int)(rating * 100);
                    return;
                }

                if (power > 0 && optionRow.SkillDamageMin > 0)
                {            
                    var valueSubMin = power - optionRow.SkillDamageMin;
                    var rating = 1 - valueSubMin / (double)(optionRow.SkillDamageMax - optionRow.SkillDamageMin);
                    ratingPercent = (int)(rating * 100);
                }
            });

            return ratingPercent;
        }
#region Effects
        /// <summary>
        /// Invoke On Animator
        /// </summary>
        [UsedImplicitly]
        public void ShowItemViewEffect()
        {
            itemViewEffect.SetActive(true);
        }

        /// <summary>
        /// Invoke On Animator
        /// </summary>
        [UsedImplicitly]
        public void ShowOptionViewEffect(int idx)
        {
            if (idx < 0)
            {
                return;
            }
            
            foreach (var statView in optionViews)
            {
                if (statView.gameObject.activeSelf || statView.IsEmpty)
                {
                    continue;
                }

                viewEffects[idx].SetActive(true);
                statView.Show();
                return;
            }

            if (skillView.IsEmpty)
            {
                return;
            }

            viewEffects[idx].SetActive(true);
            skillView.Show();
        }
        
        private void HideAllEffects()
        {
            itemViewEffect.SetActive(false);
            viewEffects.ForEach(effect => effect.SetActive(false));
        }
#endregion Effects
    }
}
