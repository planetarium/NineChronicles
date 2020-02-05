using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.State;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour, RecipeCellView.IEventListener
    {
        public RecipeScrollerController scrollerController;
        public Button closeButton;
        
        [CanBeNull] private RecipeCellView.IEventListener _eventListener;

        private void Awake()
        {
            scrollerController.RegisterListener(this);
            
            closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Hide();
            }).AddTo(gameObject);
        }

        public void Show(float scrollPos = 0)
        {
            gameObject.SetActive(true);
            
            var recipeInfoList = new List<RecipeInfo>();
            foreach (var row in Game.Game.instance.TableSheets.ConsumableItemRecipeSheet)
            {
                var info = new RecipeInfo(row);
                recipeInfoList.Add(info);
            }
            recipeInfoList.Sort((x, y) => x.Row.Id - y.Row.Id);
            scrollerController.SetData(recipeInfoList);
            scrollerController.scroller.ScrollPosition = scrollPos;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void RegisterListener(RecipeCellView.IEventListener eventListener)
        {
            _eventListener = eventListener;
        }

        public void OnRecipeCellViewStarClick(RecipeCellView recipeCellView)
        {
            _eventListener?.OnRecipeCellViewStarClick(recipeCellView);
        }

        public void OnRecipeCellViewSubmitClick(RecipeCellView recipeCellView)
        {
            _eventListener?.OnRecipeCellViewSubmitClick(recipeCellView);
        }
    }
}
