using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformation : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            public SimpleCountableItemView itemView;
            public List<Image> elementalTypeImages;
            public Text commonText;
        }

        [Serializable]
        public struct DescriptionArea
        {
            public RectTransform root;
            public Text text;
        }

        [Serializable]
        public struct StatsArea
        {
            public RectTransform root;
            public Text commonText;
            public Text levelLimitText;
            public ItemInformationStat statPrefab;
            public List<ItemInformationStat> stats;
        }

        [Serializable]
        public struct SkillsArea
        {
            public RectTransform root;
            public ItemInformationSkill skillPrefab;
            public List<ItemInformationSkill> skills;
        }

        public IconArea iconArea;
        public DescriptionArea descriptionArea;
        public StatsArea statsArea;
        public SkillsArea skillsArea;
        public SkillsArea buffSkillsArea;

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
            UpdateStatsArea();
            UpdateSkillsArea();
            UpdateBuffSkillsArea();
        }

        private void UpdateViewIconArea()
        {
            if (Model?.item.Value is null)
            {
                // 아이콘.
                iconArea.itemView.Clear();

                // 속성.
                foreach (var image in iconArea.elementalTypeImages)
                {
                    image.enabled = false;
                }

                iconArea.commonText.enabled = false;

                return;
            }

            var itemRow = Model.item.Value.ItemBase.Value.Data;

            // 아이콘.
            iconArea.itemView.SetData(Model.item.Value);

            // 속성.
            var sprite = itemRow.ElementalType.GetSprite();
            var elementalCount = itemRow.Grade;
            for (var i = 0; i < iconArea.elementalTypeImages.Count; i++)
            {
                var image = iconArea.elementalTypeImages[i];
                if (sprite is null ||
                    i >= elementalCount)
                {
                    image.enabled = false;
                    continue;
                }

                image.enabled = true;
                image.overrideSprite = sprite;
            }

            // 텍스트.
            if (Model.item.Value.ItemBase.Value.Data.ItemType == ItemType.Material)
            {
                iconArea.commonText.enabled = false;
            }
            else
            {
                // todo: 내구도가 생기면 이곳에서 표시해줘야 함.
                iconArea.commonText.enabled = false;
            }
        }

        private void UpdateDescriptionArea()
        {
            if (Model?.item.Value is null)
            {
                descriptionArea.root.gameObject.SetActive(false);

                return;
            }

            descriptionArea.text.text = Model.item.Value.ItemBase.Value.Data.GetLocalizedDescription();
            descriptionArea.root.gameObject.SetActive(true);
        }

        private void UpdateStatsArea()
        {
            if (Model?.item.Value is null)
            {
                statsArea.root.gameObject.SetActive(false);

                return;
            }

            foreach (var stat in statsArea.stats)
            {
                stat.Hide();
            }
            
            var statCount = 0;
            if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                statsArea.commonText.enabled = false;
                // todo: 장비에 레벨 제한이 들어가면 이곳에서 적용해줘야 함.
                statsArea.levelLimitText.enabled = false;

                foreach (var statMap in itemUsable.StatsMap.StatMaps)
                {
                    if (statMap.Key == StatType.SPD)
                        continue;

                    AddStat(new Model.ItemInformationStat(statMap.Value));
                    statCount++;
                }
            }
            else
            {
                statsArea.commonText.enabled = true;
                statsArea.commonText.text = LocalizationManager.Localize("UI_ADDITIONAL_ABILITIES_WHEN_COMBINED");
                statsArea.levelLimitText.enabled = false;

                var data = Model.item.Value.ItemBase.Value.Data;
                if (data.ItemType == ItemType.Material &&
                    data is MaterialItemSheet.Row materialData &&
                    materialData.StatType.HasValue)
                {
                    AddStat(new Model.ItemInformationStat(materialData));
                    statCount++;
                }
            }

            if (statCount <= 0)
            {
                statsArea.root.gameObject.SetActive(false);

                return;
            }

            statsArea.root.gameObject.SetActive(true);
        }

        private void UpdateSkillsArea()
        {
            if (Model?.item.Value is null)
            {
                skillsArea.root.gameObject.SetActive(false);

                return;
            }

            foreach (var skill in skillsArea.skills)
            {
                skill.Hide();
            }

            var skillCount = 0;
            if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                foreach (var skill in itemUsable.Skills)
                {
                    AddSkill(new Model.ItemInformationSkill(skill));
                    skillCount++;
                }
            }
            else
            {
                var data = Model.item.Value.ItemBase.Value.Data;
                if (data.ItemType == ItemType.Material &&
                    data is MaterialItemSheet.Row materialData &&
                    materialData.SkillId != 0)
                {
                    AddSkill(new Model.ItemInformationSkill(materialData));
                    skillCount++;
                }
            }

            if (skillCount <= 0)
            {
                skillsArea.root.gameObject.SetActive(false);

                return;
            }

            skillsArea.root.gameObject.SetActive(true);
        }

        private void UpdateBuffSkillsArea()
        {
            if (Model?.item.Value is null)
            {
                buffSkillsArea.root.gameObject.SetActive(false);

                return;
            }

            foreach (var skill in buffSkillsArea.skills)
            {
                skill.Hide();
            }

            var buffSkillCount = 0;
            if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                foreach (var buffSkill in itemUsable.BuffSkills)
                {
                    AddBuffSkill(new Model.ItemInformationSkill(buffSkill));
                    buffSkillCount++;
                }
            }

            if (buffSkillCount <= 0)
            {
                buffSkillsArea.root.gameObject.SetActive(false);

                return;
            }

            buffSkillsArea.root.gameObject.SetActive(true);
        }

        private void AddStat(Model.ItemInformationStat model)
        {
            foreach (var stat in statsArea.stats)
            {
                if (stat.IsShow)
                {
                    continue;
                }

                stat.Show(model);

                return;
            }

            var go = Instantiate(statsArea.statPrefab.gameObject, statsArea.root);
            var comp = go.GetComponent<ItemInformationStat>();
            statsArea.stats.Add(comp);
            comp.Show(model);
        }

        private void AddSkill(Model.ItemInformationSkill model)
        {
            foreach (var skill in skillsArea.skills.Where(skill => !skill.IsShow))
            {
                skill.Show(model);

                return;
            }

            var go = Instantiate(skillsArea.skillPrefab.gameObject, skillsArea.root);
            var comp = go.GetComponent<ItemInformationSkill>();
            skillsArea.skills.Add(comp);
            comp.Show(model);
        }

        private void AddBuffSkill(Model.ItemInformationSkill model)
        {
            foreach (var skill in buffSkillsArea.skills.Where(skill => !skill.IsShow))
            {
                skill.Show(model);

                return;
            }

            var go = Instantiate(buffSkillsArea.skillPrefab.gameObject, buffSkillsArea.root);
            var comp = go.GetComponent<ItemInformationSkill>();
            buffSkillsArea.skills.Add(comp);
            comp.Show(model);
        }
    }
}
