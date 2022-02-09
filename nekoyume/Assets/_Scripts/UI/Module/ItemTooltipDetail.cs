using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    public class ItemTooltipDetail : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            public TextMeshProUGUI gradeText;
            public Image gradeAndSubTypeSpacer;
            public TextMeshProUGUI subTypeText;
            public GameObject combatPowerObject;
            public TextMeshProUGUI combatPowerText;
            public GameObject elementalTypeObject;
            public Image elementalTypeImage;
            public TextMeshProUGUI elementalTypeText;
            public GameObject countObject;
            public TextMeshProUGUI countText;
        }

        [Serializable]
        public struct DescriptionArea
        {
            public GameObject itemDescriptionGameObject;
            public TextMeshProUGUI itemDescriptionText;
            public GameObject levelLimitGameObject;
            public TextMeshProUGUI levelLimitText;
        }

        [Serializable]
        public struct StatRow
        {
            public StatView StatView;
            public List<GameObject> StarImages;
        }

        [SerializeField]
        private TextMeshProUGUI itemName;

        [SerializeField]
        private TooltipItemView itemView;

        [SerializeField]
        private IconArea iconArea;

        [SerializeField]
        private DescriptionArea descriptionArea;

        [SerializeField]
        private StatView statView;

        [SerializeField]
        private GameObject optionSpacer;

        [SerializeField]
        private GameObject optionAreaRoot;

        [SerializeField]
        private TextMeshProUGUI tradableText;

        [SerializeField]
        private List<StatRow> statRows;

        [SerializeField]
        private List<SkillView> skills;

        public void Set(ItemBase itemBase, int itemCount)
        {
            UpdateViewIconArea(itemBase, itemCount);
            UpdateDescriptionArea(itemBase);
            UpdateOptionArea(itemBase, itemCount);
            UpdateTradableText(itemBase);
        }

        private void UpdateOptionArea(ItemBase itemBase, int itemCount)
        {
            var statCount = UpdateStatsArea(itemBase, itemCount);
            var skillCount = UpdateSkillsArea(itemBase);
            var hasOptions = statView.gameObject.activeSelf || statCount + skillCount > 0;
            optionSpacer.SetActive(hasOptions);
            optionAreaRoot.SetActive(hasOptions);
        }

        private void UpdateViewIconArea(ItemBase itemBase, int itemCount)
        {
            itemName.text = itemBase.GetLocalizedName(false);
            itemView.Set(itemBase, itemCount);

            var gradeColor = itemBase.GetItemGradeColor();
            iconArea.gradeText.text = itemBase.GetGradeText();
            iconArea.gradeText.color = gradeColor;
            iconArea.subTypeText.text = itemBase.GetSubTypeText();
            iconArea.subTypeText.color = gradeColor;
            iconArea.gradeAndSubTypeSpacer.color = gradeColor;

            var sprite = itemBase.ElementalType.GetSprite();
            if (sprite is null || !itemBase.ItemType.HasElementType())
            {
                iconArea.elementalTypeObject.SetActive(false);
                return;
            }

            iconArea.elementalTypeText.text = itemBase.ElementalType.GetLocalizedString();
            iconArea.elementalTypeText.color = itemBase.GetElementalTypeColor();
            iconArea.elementalTypeImage.overrideSprite = sprite;
            iconArea.elementalTypeObject.SetActive(true);
        }

        private void UpdateDescriptionArea(ItemBase itemBase)
        {
            descriptionArea.itemDescriptionGameObject.SetActive(true);
            descriptionArea.itemDescriptionText.text = itemBase.GetLocalizedDescription();
        }

        private int UpdateStatsArea(ItemBase itemBase, int itemCount)
        {
            var statCount = 0;
            if (itemBase is null)
            {
                return statCount;
            }

            foreach (var row in statRows)
            {
                row.StatView.gameObject.SetActive(false);
                row.StarImages.ForEach(x => x.SetActive(false));
            }

            var isUsable = Util.IsUsableItem(itemBase.Id);
            var level = Util.GetItemRequirementLevel(itemBase.Id);
            descriptionArea.levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", level);
            descriptionArea.levelLimitText.color = isUsable ?
                Palette.GetColor(ColorType.ButtonEnabled) : Palette.GetColor(ColorType.TextDenial);
            descriptionArea.levelLimitGameObject.SetActive(level > 0);

            switch (itemBase)
            {
                case Equipment equipment:
                {
                    iconArea.combatPowerObject.SetActive(true);
                    iconArea.combatPowerText.text = equipment.GetCPText();
                    iconArea.countObject.SetActive(false);

                    var optionInfo = new ItemOptionInfo(equipment);
                    var (mainStatType, _, mainStatTotalValue) = optionInfo.MainStat;
                    statView.Show(mainStatType, mainStatTotalValue);

                    foreach (var (type, value, count) in optionInfo.StatOptions)
                    {
                        AddStat(type, value, count);
                        statCount += count;
                    }

                    break;
                }
                case ItemUsable itemUsable:
                {
                    iconArea.combatPowerObject.SetActive(false);
                    iconArea.countObject.SetActive(false);
                    statView.gameObject.SetActive(false);

                    foreach (var statMapEx in itemUsable.StatsMap.GetStats())
                    {
                        AddStat(statMapEx);
                        statCount++;
                    }

                    break;
                }
                case Costume costume:
                {
                    statView.gameObject.SetActive(false);
                    var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                    iconArea.countObject.SetActive(false);
                    var statsMap = new StatsMap();
                    foreach (var row in costumeSheet.OrderedList.Where(r => r.CostumeId == costume.Id))
                    {
                        statsMap.AddStatValue(row.StatType, row.Stat);
                    }

                    foreach (var statMapEx in statsMap.GetStats())
                    {
                        AddStat(statMapEx);
                        statCount++;
                    }

                    var cpEnable = statCount > 0;
                    iconArea.combatPowerObject.SetActive(cpEnable);
                    if (cpEnable)
                    {
                        iconArea.combatPowerText.text = costume.GetCPText(costumeSheet);
                    }

                    break;
                }
                case Material _:
                {
                    statView.gameObject.SetActive(false);
                    iconArea.combatPowerObject.SetActive(false);

                    var countFormat = L10nManager.Localize("UI_COUNT_FORMAT");
                    var countString = string.Format(countFormat, itemCount);
                    iconArea.countText.text = countString;
                    iconArea.countObject.SetActive(true);
                    break;
                }
                default:
                    statView.gameObject.SetActive(false);
                    iconArea.combatPowerObject.SetActive(false);
                    iconArea.countObject.SetActive(false);
                    break;
            }

            return statCount;
        }

        private void AddStat(StatMapEx model)
        {
            var statView = GetDisabledStatRow();
            if (statView.Equals(default) ||
                statView.StatView is null)
                throw new NotFoundComponentException<StatView>();

            statView.StatView.Show(model);
            var starImage = statView.StarImages.FirstOrDefault();
            if (starImage is null)
            {
                Debug.LogError("Failed to get star image for option.");
                return;
            }
            starImage.SetActive(true);
        }

        private void AddStat(StatType statType, int value, int count)
        {
            var statView = GetDisabledStatRow();
            if (statView.Equals(default) ||
                statView.StatView is null)
                throw new NotFoundComponentException<StatView>();
            statView.StatView.Show(statType, value, true);

            for (var i = 0; i < count; ++i)
            {
                var starImage = statView.StarImages.FirstOrDefault(x => !x.activeSelf);
                if (starImage is null)
                {
                    Debug.LogError("Failed to get star image for option.");
                    return;
                }

                starImage.SetActive(true);
            }
        }

        private StatRow GetDisabledStatRow()
        {
            foreach (var stat in statRows)
            {
                if (stat.StatView.gameObject.activeSelf)
                {
                    continue;
                }

                return stat;
            }

            return default;
        }

        private int UpdateSkillsArea(ItemBase itemBase)
        {
            var skillCount = 0;

            if (itemBase is null)
            {
                return skillCount;
            }

            foreach (var skill in skills)
            {
                skill.Hide();
            }

            if (!(itemBase is ItemUsable itemUsable))
            {
                return skillCount;
            }

            foreach (var skill in itemUsable.Skills)
            {
                AddSkill(new Model.SkillView(skill));
                skillCount++;
            }

            foreach (var skill in itemUsable.BuffSkills)
            {
                AddSkill(new Model.SkillView(skill));
                skillCount++;
            }

            return skillCount;
        }

        private void AddSkill(Model.SkillView model)
        {
            foreach (var skill in skills.Where(skill => !skill.IsShown))
            {
                skill.SetData(model);
                skill.Show();

                return;
            }
        }

        private void UpdateTradableText(ItemBase itemBase)
        {
            var isTradable = itemBase is ITradableItem;
            tradableText.text = isTradable
                ? L10nManager.Localize("UI_TRADABLE")
                : L10nManager.Localize("UI_UNTRADABLE");
            tradableText.color = isTradable
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }
    }
}
