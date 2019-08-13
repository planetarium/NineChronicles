using EnhancedUI.EnhancedScroller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class RecipeCellView : EnhancedScrollerCellView
    {
        [Serializable]
        public class MaterialIcon
        {
            public Image image;
            public Text mark;
        }

        public Text resultNameText;
        public Button combineButton;
        public IObservable<Unit> combineButtonOnClick;
        public IDisposable onClickDisposable;
        public Text combineText;
        public SimpleCountableItemView resultItemView;
        public SimpleCountableItemView[] materialItemViews;

        private const float ResultIconScaleFactor = 1.2f;
        private const float MaterialIconScaleFactor = 0.7f;
        private readonly Color DimmedColor = new Color(0.3f, 0.3f, 0.3f);

        public RecipeInfo Model { get; private set; }

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
            combineButtonOnClick = combineButton.OnClickAsObservable();
        }

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(RecipeInfo recipeInfo)
        {
            Model = recipeInfo;
            resultNameText.text = recipeInfo.resultName;

            SetItemView(recipeInfo.resultId, recipeInfo.resultAmount, resultItemView, ResultIconScaleFactor, false);

            for (int i = 0; i < recipeInfo.materialInfos.Length; ++i)
            {
                var info = recipeInfo.materialInfos[i];
                if (info.id == 0) break;
                SetItemView(info.id, info.amount, materialItemViews[i], MaterialIconScaleFactor, true, !info.isEnough);
            }

            foreach(var info in recipeInfo.materialInfos)
            {
                if (info.id != 0 && !info.isEnough)
                {
                    combineButton.enabled = false;
                    combineButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                    combineText.color = ColorHelper.HexToColorRGB("92A3B5");
                    break;
                }
            }
        }

        public void SetItemView(int itemId, int amount, SimpleCountableItemView itemView, float scaleFactor, bool isMaterial, bool isDimmed = false)
        {
            var item = isMaterial ? ItemFactory.CreateMaterial(itemId, new Guid()) : ItemFactory.CreateEquipment(itemId, new Guid());
            var countableItem = new CountableItem(item, amount);
            try
            {
                itemView.SetData(countableItem);
            }
            catch (FailedToLoadResourceException<Sprite> e)
            {
                Debug.LogWarning($"No sprite : {itemId} {e}");
            }
            itemView.Model.dimmed.Value = isDimmed;
            var rect = itemView.iconImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * scaleFactor, rect.sizeDelta.y * scaleFactor);
            itemView.gameObject.SetActive(true);
        }

        public void Clear()
        {
            resultItemView.Clear();
            for (int i = 0; i < materialItemViews.Length; ++i)
            {
                materialItemViews[i].Clear();
                materialItemViews[i].gameObject.SetActive(false);
                combineButton.enabled = true;
                combineButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                combineText.color = Color.white;
            }
        }
    }
}
