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
            public Text valuesText;
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
            if (Model == null)
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
                if (sprite == null ||
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
            if (Model == null)
            {
                statsArea.dividerImage.enabled = false;
                statsArea.keysText.enabled = false;
                statsArea.valuesText.enabled = false;
                
                return;
            }
            
//            var stats = ((ItemUsable) Data.item.Value).
            statsArea.dividerImage.enabled = true;
            statsArea.keysText.text = "key 1\nkey 2";
            statsArea.keysText.enabled = true;
            statsArea.valuesText.text = "value 1\nvalue 2";
            statsArea.valuesText.enabled = true;
        }

        private void UpdateDescriptionArea()
        {
            if (Model == null)
            {
                descriptionArea.dividerImage.enabled = false;
                descriptionArea.text.enabled = false;
                
                return;
            }
            
            descriptionArea.dividerImage.enabled = true;
            descriptionArea.text.text = Model.item.Value.item.Value.Data.description;
            descriptionArea.text.enabled = true;
        }
        
        private string GetOptionalTitleText(string cls)
        {
            return "OptionalTitleText";
        }
        
        private string GetOptionalDescriptionText(string cls)
        {
            return "OptionalDescriptionText";
        }
    }
}
