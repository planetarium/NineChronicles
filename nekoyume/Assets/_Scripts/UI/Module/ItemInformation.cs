using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    public class ItemInformation : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            public SimpleCountableItemView itemView;
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

        public IconArea iconArea;
        public DescriptionArea descriptionArea;

        [SerializeField]
        private GameObject optionSpacer = null;

        [SerializeField]
        private GameObject optionAreaRoot = null;

        public StatView uniqueStat;
        public List<StatRow> statRows;
        public List<SkillView> skills;

        [SerializeField]
        private TextMeshProUGUI tradableText = null;

        public Model.ItemInformation Model { get; private set; }

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void SetData(Model.ItemInformation data)
        {
            if (data == null)
            {
                Clear();

                return;
            }

            _disposables.DisposeAllAndClear();
            Model = data;

            UpdateView();
        }

        public void Clear()
        {
            _disposables.DisposeAllAndClear();
            Model?.Dispose();
            Model = null;

            UpdateView();
        }

        private void UpdateView()
        {
            UpdateViewIconArea();
            UpdateDescriptionArea();
            var statCount = UpdateStatsArea();
            var skillCount = UpdateSkillsArea();
            var hasOptions = uniqueStat.gameObject.activeSelf || statCount + skillCount > 0;
            optionSpacer.SetActive(hasOptions);
            optionAreaRoot.SetActive(hasOptions);

            var isTradable = Model.item.Value.ItemBase.Value is ITradableItem;
            tradableText.text = isTradable ?
                L10nManager.Localize("UI_TRADABLE") : L10nManager.Localize("UI_UNTRADABLE");
            tradableText.color = isTradable ?
                Palette.GetColor(ColorType.ButtonEnabled) : Palette.GetColor(ColorType.TextDenial);
        }

        private void UpdateViewIconArea()
        {
            if (Model?.item.Value is null)
            {
                // 아이콘.
                iconArea.itemView.Clear();
                iconArea.elementalTypeObject.SetActive(false);

                return;
            }

            var item = Model.item.Value.ItemBase.Value;


            // 아이콘.
            iconArea.itemView.SetData(new CountableItem(
                Model.item.Value.ItemBase.Value,
                Model.item.Value.Count.Value));

            var gradeColor = item.GetItemGradeColor();
            iconArea.gradeText.text = item.GetGradeText();
            iconArea.gradeText.color = gradeColor;
            iconArea.subTypeText.text = item.GetSubTypeText();
            iconArea.subTypeText.color = gradeColor;
            iconArea.gradeAndSubTypeSpacer.color = gradeColor;

            // 속성.
            var sprite = item.ElementalType.GetSprite();
            if (sprite is null || !item.ItemType.HasElementType())
            {
                iconArea.elementalTypeObject.SetActive(false);
                return;
            }

            iconArea.elementalTypeText.text = item.ElementalType.GetLocalizedString();
            iconArea.elementalTypeText.color = item.GetElementalTypeColor();
            iconArea.elementalTypeImage.overrideSprite = sprite;
            iconArea.elementalTypeObject.SetActive(true);
        }

        private void UpdateDescriptionArea()
        {
            if (Model?.item.Value is null)
            {
                descriptionArea.itemDescriptionGameObject.SetActive(false);

                return;
            }

            descriptionArea.itemDescriptionGameObject.SetActive(true);
            descriptionArea.itemDescriptionText.text = Model.item.Value.ItemBase.Value.GetLocalizedDescription();
        }

        private int UpdateStatsArea()
        {
            if (Model?.item.Value is null)
            {
                return 0;
            }

            foreach (var row in statRows)
            {
                row.StatView.gameObject.SetActive(false);
                row.StarImages.ForEach(x => x.SetActive(false));
            }

            var statCount = 0;
            if (Model.item.Value.ItemBase.Value is Equipment equipment)
            {
                descriptionArea.levelLimitGameObject.SetActive(false);
                iconArea.combatPowerObject.SetActive(true);
                iconArea.combatPowerText.text = equipment.GetCPText();
                iconArea.countObject.SetActive(false);

                var optionInfo = new ItemOptionInfo(equipment);
                var (mainStatType, _, mainStatTotalValue) = optionInfo.MainStat;
                uniqueStat.Show(mainStatType, mainStatTotalValue);

                foreach (var (type, value, count) in optionInfo.StatOptions)
                {
                    AddStat(type, value, count);
                    statCount += count;
                }
            }
            else if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                descriptionArea.levelLimitGameObject.SetActive(false);
                iconArea.combatPowerObject.SetActive(false);
                iconArea.countObject.SetActive(false);
                uniqueStat.gameObject.SetActive(false);

                foreach (var statMapEx in itemUsable.StatsMap.GetStats())
                {
                    AddStat(statMapEx);
                    statCount++;
                }
            }
            else if (Model.item.Value.ItemBase.Value is Costume costume)
            {
                uniqueStat.gameObject.SetActive(false);
                var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                descriptionArea.levelLimitGameObject.SetActive(false);
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
            }
            else if (Model.item.Value.ItemBase.Value is Material)
            {
                uniqueStat.gameObject.SetActive(false);
                descriptionArea.levelLimitGameObject.SetActive(false);
                iconArea.combatPowerObject.SetActive(false);

                var countFormat = L10nManager.Localize("UI_COUNT_FORMAT");
                var countString = string.Format(countFormat, Model.item.Value.Count.Value);
                iconArea.countText.text = countString;
                iconArea.countObject.SetActive(true);
            }
            else
            {
                uniqueStat.gameObject.SetActive(false);
                descriptionArea.levelLimitGameObject.SetActive(false);
                iconArea.combatPowerObject.SetActive(false);
                iconArea.countObject.SetActive(false);
            }

            return statCount;
        }

        private int UpdateSkillsArea()
        {
            if (Model?.item.Value is null)
            {
                return 0;
            }

            foreach (var skill in skills)
            {
                skill.Hide();
            }

            var skillCount = 0;
            if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
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
            }

            return skillCount;
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

            for (int i = 0; i < count; ++i)
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


        private void AddSkill(Model.SkillView model)
        {
            foreach (var skill in skills.Where(skill => !skill.IsShown))
            {
                skill.SetData(model);
                skill.Show();

                return;
            }
        }
    }
}
