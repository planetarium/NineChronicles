using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour
    {
        public RecipeScrollerController scrollerController;
        public Button closeButton;

        private void Awake()
        {
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
            foreach (var row in TableSheetsState.Current.ConsumableItemRecipeSheet)
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
    }
}
