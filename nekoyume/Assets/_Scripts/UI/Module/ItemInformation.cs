using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.Serialization;
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

            var itemRow = Model.item.Value.item.Value.Data;

            // 아이콘.
            iconArea.itemView.SetData(Model.item.Value);

            // 속성.
            var sprite = itemRow.elemental.GetSprite();
            var elementalCount = itemRow.grade;
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
            if (Model.item.Value.item.Value.Data.cls.ToEnumItemType() == ItemBase.ItemType.Material)
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

            descriptionArea.text.text = Model.item.Value.item.Value.Data.LocalizedDescription;
            descriptionArea.root.gameObject.SetActive(true);
        }

        private void UpdateStatsArea()
        {
            if (Model?.item.Value is null)
            {
                statsArea.root.gameObject.SetActive(false);

                return;
            }

            RemoveStatAll();
            var statCount = 0;
            if (Model.item.Value.item.Value is ItemUsable itemUsable)
            {
                statsArea.commonText.enabled = false;
                // todo: 장비에 레벨 제한이 들어가면 이곳에서 적용해줘야 함.
                statsArea.levelLimitText.enabled = false;
                
                foreach (var statMap in itemUsable.Stats.StatMaps)
                {
                    if (statMap.Key.Equals("turnSpeed")
                        || statMap.Key.Equals("attackRange"))
                    {
                        continue;
                    }

                    AddStat(new Model.ItemInformationStat(statMap.Value));
                    statCount++;
                }
            }
            else
            {
                statsArea.commonText.enabled = true;
                statsArea.commonText.text = LocalizationManager.Localize("UI_ADDITIONAL_ABILITIES_WHEN_COMBINED");
                statsArea.levelLimitText.enabled = false;
                
                var data = Model.item.Value.item.Value.Data;
                if (!string.IsNullOrEmpty(data.stat))
                {
                    AddStat(new Model.ItemInformationStat(data));
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

            RemoveSkillAll();
            var skillCount = 0;
            if (Model.item.Value.item.Value is ItemUsable itemUsable)
            {
                foreach (var skill in itemUsable.Skills)
                {
                    AddSkill(new Model.ItemInformationSkill(skill));
                    skillCount++;
                }
            }
            else
            {
                var itemRow = Model.item.Value.item.Value.Data;
                if (itemRow.skillId != 0)
                {
                    AddSkill(new Model.ItemInformationSkill(itemRow));
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
        
        private void RemoveStatAll()
        {
            foreach (var stat in statsArea.stats)
            {
                stat.Hide();
            }
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

        private void RemoveSkillAll()
        {
            foreach (var skill in skillsArea.skills)
            {
                skill.Hide();
            }
        }

        private void AddSkill(Model.ItemInformationSkill model)
        {
            foreach (var skill in skillsArea.skills)
            {
                if (skill.IsShow)
                {
                    continue;
                }

                skill.Show(model);

                return;
            }

            var go = Instantiate(skillsArea.skillPrefab.gameObject, skillsArea.root);
            var comp = go.GetComponent<ItemInformationSkill>();
            skillsArea.skills.Add(comp);
            comp.Show(model);
        }
    }
}
