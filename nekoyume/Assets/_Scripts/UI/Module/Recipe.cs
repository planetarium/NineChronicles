using Nekoyume.Data;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour
    {
        public RecipeScrollerController scrollerController;

        public void Reload(float scrollPos = 0)
        {
            var recipeInfoList = new List<RecipeInfo>();
            var recipeTable = Tables.instance.Recipe;
            foreach (var pair in recipeTable)
            {
                var info = new RecipeInfo(pair.Value.ResultId,
                    pair.Value.Material1,
                    pair.Value.Material2,
                    pair.Value.Material3,
                    pair.Value.Material4,
                    pair.Value.Material5);

                recipeInfoList.Add(info);
            }
            recipeInfoList.Sort((x, y) => x.recipeId - y.recipeId);
            scrollerController.SetData(recipeInfoList);
            scrollerController.scroller.ScrollPosition = scrollPos;
        }
    }
}
