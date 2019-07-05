using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
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
            public Text optionTitleText;
            public Text optionDescriptionText;
        }

        [Serializable]
        public struct StatsArea
        {
            public Image dividerImage;
            public Text keysText;
        }

        [Serializable]
        public struct DescriptionArea
        {
            public Image dividerImage;
            public Text text;
        }

        public IconArea iconArea;
        public StatsArea statsArea;
        public DescriptionArea descriptionArea;

        public Model.ItemInformation Model { get; private set; }

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void SetData(Model.ItemInformation data)
        {
            _disposables.DisposeAllAndClear();
            Model = data;

            UpdateView();
        }

        public void Clear()
        {
            _disposables.DisposeAllAndClear();
            Model = null;

            UpdateView();
        }

        private void UpdateView()
        {
            UpdateViewIconArea();
            UpdateStatsArea();
            UpdateDescriptionArea();
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
                
                // 스킬..?
                iconArea.optionTitleText.enabled = false;
                iconArea.optionDescriptionText.enabled = false;
                
                return;
            }
            
            var itemRow = Model.item.Value.item.Value.Data;
            
            // 아이콘.
            iconArea.itemView.SetData(Model.item.Value);
            
            // 속성.
            var sprite = Elemental.GetSprite(itemRow.elemental);
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

                image.sprite = sprite;
                image.enabled = true;
            }

            // 스킬.
            if (Model.optionalEnabled.Value)
            {
                iconArea.optionTitleText.text = GetOptionalTitleText(itemRow.cls);
                iconArea.optionTitleText.enabled = true;
                iconArea.optionDescriptionText.text = GetOptionalDescriptionText(itemRow.cls);
                iconArea.optionDescriptionText.enabled = true;
            }
            else
            {
                iconArea.optionTitleText.enabled = false;
                iconArea.optionDescriptionText.enabled = false;
            }
        }

        private void UpdateStatsArea()
        {
            if (Model?.item.Value is null)
            {
                statsArea.dividerImage.enabled = false;
                statsArea.keysText.enabled = false;
                
                return;
            }

            var itemInfo = Model.item.Value.item.Value.ToItemInfo();
            if (string.IsNullOrEmpty(itemInfo))
            {
                statsArea.dividerImage.enabled = false;
                statsArea.keysText.enabled = false;
                
                return;
            }
            
            statsArea.dividerImage.enabled = true;
            statsArea.keysText.text = itemInfo;
            statsArea.keysText.enabled = true;
        }

        private void UpdateDescriptionArea()
        {
            if (Model?.item.Value is null)
            {
                descriptionArea.dividerImage.enabled = false;
                descriptionArea.text.enabled = false;
                
                return;
            }
            
            descriptionArea.dividerImage.enabled = true;
            descriptionArea.text.text = Model.item.Value.item.Value.Data.description;
            descriptionArea.text.enabled = true;
        }
        
        // ToDo. 아이템에 따라 다른 정보를 그려줘야 함.
        private string GetOptionalTitleText(string cls)
        {
            return "OptionalTitleText";
        }
        
        // ToDo. 아이템에 따라 다른 정보를 그려줘야 함.
        private string GetOptionalDescriptionText(string cls)
        {
            return "OptionalDescriptionText";
        }
    }
}
