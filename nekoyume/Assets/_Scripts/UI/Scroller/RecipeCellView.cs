using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System;
using System.Linq;
using Assets.SimpleLocalization;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class RecipeCellView : EnhancedScrollerCellView
    {
        public interface IEventListener
        {
            void OnRecipeCellViewStarClick(RecipeCellView recipeCellView);
            void OnRecipeCellViewSubmitClick(RecipeCellView recipeCellView);
        }
        
        public Button starButton;
        public SimpleItemView resultItemView;
        public TextMeshProUGUI resultItemNameText;
        public SimpleItemView[] materialItemViews;
        public SubmitButton submitButton;
        public TextMeshProUGUI submitText;
        
        public RecipeInfo Model { get; private set; }

        [CanBeNull] private IEventListener _eventListener;
        
        private void Awake()
        {
            starButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                _eventListener?.OnRecipeCellViewStarClick(this);
            }).AddTo(gameObject);
            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                _eventListener?.OnRecipeCellViewSubmitClick(this);
            }).AddTo(gameObject);
            submitText.text = LocalizationManager.Localize("UI_SELECT");
        }

        public void RegisterListener(IEventListener eventListener)
        {
            _eventListener = eventListener;
        }
        
        public void SetData(RecipeInfo recipeInfo)
        {
            if (recipeInfo is null)
            {
                Clear();
                return;
            }
            
            Model = recipeInfo;
            if (Model.IsLocked)
            {
                resultItemNameText.text = "?";
                resultItemView.SetToUnknown();
                
                var materialInfosCount = Model.MaterialInfos.Count;
                for (var i = 0; i < materialItemViews.Length; i++)
                {
                    var view = materialItemViews[i];
                    if (i < materialInfosCount)
                    {
                        view.Show();
                        view.SetToUnknown();
                    }
                    else
                    {
                        view.Hide();
                    }
                }
                
                submitButton.SetSubmittable(false);
                submitText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
            else
            {
                resultItemNameText.text = Model.ResultItemName;
                SetItemView(Model.Row.ResultConsumableItemId, resultItemView);
                
                var materialInfosCount = Model.MaterialInfos.Count;
                for (var i = 0; i < materialItemViews.Length; i++)
                {
                    var view = materialItemViews[i];
                    if (i < materialInfosCount)
                    {
                        var info = Model.MaterialInfos[i];
                        view.Show();
                        SetItemView(info.Id, view, !info.IsEnough);    
                    }
                    else
                    {
                        view.Hide();
                    }
                }
                
                if (Model.MaterialInfos.Any(info => info.Id != 0 && !info.IsEnough))
                {
                    submitButton.SetSubmittable(false);
                    submitText.color = ColorHelper.HexToColorRGB("92A3B5");
                }
                else
                {
                    submitButton.SetSubmittable(true);
                    submitText.color = Color.white;
                }
            }
        }

        private void SetItemView(int itemId, SimpleItemView itemView, bool isDimmed = false)
        {
            var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(i => i.Id == itemId);
            itemView.SetData(new Item(ItemFactory.Create(row, new Guid())));
            itemView.Model.Dimmed.Value = isDimmed;
            itemView.gameObject.SetActive(true);
        }

        private void Clear()
        {
            resultItemView.Clear();
            for (var i = 0; i < materialItemViews.Length; ++i)
            {
                materialItemViews[i].Clear();
                materialItemViews[i].gameObject.SetActive(false);
                submitButton.SetSubmittable(false);
                submitText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }
    }
}
