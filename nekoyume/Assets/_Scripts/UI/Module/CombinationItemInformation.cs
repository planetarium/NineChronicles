using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationItemInformation : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            public TextMeshProUGUI titleText;
            public SimpleCountableItemView itemView;
            public List<Image> elementalTypeImages;
        }

        [Serializable]
        public struct StatsArea
        {
            public RectTransform root;
            public List<BulletedStatView> stats;
        }

        public IconArea iconArea;
        public StatsArea statsArea;

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
                    image.gameObject.SetActive(false);
                }

                return;
            }

            var item = Model.item.Value.ItemBase.Value;

            // 아이콘.
            iconArea.itemView.SetData(new CountableItem(
                Model.item.Value.ItemBase.Value,
                Model.item.Value.Count.Value));

            // 속성.
            var sprite = item.ElementalType.GetSprite();
            var elementalCount = item.Grade;
            for (var i = 0; i < iconArea.elementalTypeImages.Count; i++)
            {
                var image = iconArea.elementalTypeImages[i];
                if (sprite is null ||
                    i >= elementalCount)
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                image.gameObject.SetActive(true);
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
            if (Model.item.Value.ItemBase.Value is Equipment equipment)
            {
                var uniqueStatType = equipment.UniqueStatType;
                foreach (var statMapEx in equipment.StatsMap.GetStats())
                {
                    if (!statMapEx.StatType.Equals(uniqueStatType))
                        continue;

                    AddStat(statMapEx, true);
                    statCount++;
                }

                foreach (var statMapEx in equipment.StatsMap.GetStats())
                {
                    if (statMapEx.StatType.Equals(uniqueStatType))
                        continue;

                    AddStat(statMapEx);
                    statCount++;
                }
            }
            else if (Model.item.Value.ItemBase.Value is ItemUsable itemUsable)
            {
                foreach (var statMapEx in itemUsable.StatsMap.GetStats())
                {
                    AddStat(statMapEx);
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

        private void AddStat(StatMapEx model, bool isMainStat = false)
        {
            var statView = GetDisabledStatView();
            if (statView is null)
                throw new NotFoundComponentException<BulletedStatView>();
            statView.Show(model, isMainStat);
        }

        private BulletedStatView GetDisabledStatView()
        {
            foreach (var stat in statsArea.stats)
            {
                if (stat.IsShow)
                {
                    continue;
                }

                return stat;
            }

            return null;
        }
    }
}
