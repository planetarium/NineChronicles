using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationItemInformation : MonoBehaviour
    {
        [Serializable]
        public struct TitleArea
        {
            public TextMeshProUGUI titleText;
            public SimpleCountableItemView itemView;
            public List<Image> elementalTypeImages;
        }

        [Serializable]
        public struct StatsArea
        {
            public RectTransform root;
            public TextMeshProUGUI commonText;
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

        public TitleArea titleArea;
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
            UpdateStatsArea();
            UpdateSkillsArea();
            UpdateBuffSkillsArea();
        }

        private void UpdateViewIconArea()
        {
            if (Model?.item.Value is null)
            {
                // 아이콘.
                titleArea.itemView.Clear();

                // 속성.
                foreach (var image in titleArea.elementalTypeImages)
                {
                    image.enabled = false;
                }

                return;
            }

            var itemRow = Model.item.Value.ItemBase.Value.Data;

            // 아이콘.
            titleArea.itemView.SetData(new CountableItem(
                Model.item.Value.ItemBase.Value,
                Model.item.Value.Count.Value));

            // 속성.
            var sprite = itemRow.ElementalType.GetSprite();
            var elementalCount = itemRow.Grade;
            for (var i = 0; i < titleArea.elementalTypeImages.Count; i++)
            {
                var image = titleArea.elementalTypeImages[i];
                if (sprite is null ||
                    i >= elementalCount)
                {
                    image.enabled = false;
                    continue;
                }

                image.enabled = true;
                image.overrideSprite = sprite;
            }
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

                foreach (var statMap in itemUsable.StatsMap.StatMaps.Where(e => e.Value.TotalValueAsInt > 0))
                {
                    AddStat(new Model.ItemInformationStat(statMap.Value));
                    statCount++;
                }
            }
            else
            {
                statsArea.commonText.enabled = true;
                statsArea.commonText.text = LocalizationManager.Localize("UI_ADDITIONAL_ABILITIES_WHEN_COMBINED");

                var data = Model.item.Value.ItemBase.Value.Data;
                if (data.ItemType == ItemType.Material &&
                    data is MaterialItemSheet.Row materialData &&
                    materialData.StatType != StatType.NONE)
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
