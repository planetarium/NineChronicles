using EnhancedUI.EnhancedScroller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using TMPro;
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

        private const float ResultIconScaleFactor = 1.2f;
        private const float MaterialIconScaleFactor = 0.7f;
        private static readonly Color DimmedColor = new Color(0.3f, 0.3f, 0.3f);
        
        public SimpleCountableItemView resultItemView;
        public TextMeshProUGUI resultItemNameText;
        public SimpleCountableItemView[] materialItemViews;
        public Button submitButton;
        public TextMeshProUGUI submitText;
        
        public IObservable<Unit> submitButtonOnClick;
        public IDisposable onClickDisposable;

        public RecipeInfo Model { get; private set; }

        #region Mono

        private void Awake()
        {
            submitButtonOnClick = submitButton.OnClickAsObservable();
            submitText.text = LocalizationManager.Localize("UI_RECIPE_COMBINATION");
        }

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(RecipeInfo recipeInfo)
        {
            Model = recipeInfo;
            resultItemNameText.text = recipeInfo.ResultItemName;

            SetItemView(recipeInfo.Row.ResultConsumableItemId, recipeInfo.ResultItemAmount, resultItemView, ResultIconScaleFactor, false);

            var materialInfosCount = recipeInfo.MaterialInfos.Count;
            for (var i = 0; i < materialItemViews.Length; i++)
            {
                if (i >= materialInfosCount)
                    break;

                var info = recipeInfo.MaterialInfos[i];
                SetItemView(info.Id, info.Amount, materialItemViews[i], MaterialIconScaleFactor, true, !info.IsEnough);
            }

            if (recipeInfo.MaterialInfos.Any(info => info.Id != 0 && !info.IsEnough) ||
                States.Instance.CurrentAvatarState.Value.actionPoint < GameConfig.CombineConsumableCostAP)
            {
                submitButton.enabled = false;
                submitButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                submitText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }

        private void SetItemView(int itemId, int amount, SimpleCountableItemView itemView, float scaleFactor, bool isMaterial, bool isDimmed = false)
        {
            var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(i => i.Id == itemId);
            var item = ItemFactory.Create(row, new Guid());
            var countableItem = new CountableItem(item, amount);
            try
            {
                itemView.SetData(countableItem);
            }
            catch (FailedToLoadResourceException<Sprite> e)
            {
                Debug.LogWarning($"No sprite : {itemId} {e}");
            }
            itemView.Model.Dimmed.Value = isDimmed;
            var rect = itemView.iconImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * scaleFactor, rect.sizeDelta.y * scaleFactor);
            itemView.gameObject.SetActive(true);
        }

        private void Clear()
        {
            resultItemView.Clear();
            for (var i = 0; i < materialItemViews.Length; ++i)
            {
                materialItemViews[i].Clear();
                materialItemViews[i].gameObject.SetActive(false);
                submitButton.enabled = true;
                submitButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                submitText.color = Color.white;
            }
        }
    }
}
