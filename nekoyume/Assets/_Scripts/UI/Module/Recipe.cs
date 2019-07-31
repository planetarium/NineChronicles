using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Recipe : MonoBehaviour
    {
        public RecipeScrollerController scrollerController;
        public RectTransform content;
        public RecipeCellView[] slots;

        public void Show()
        {
            scrollerController.SetData();
        }
    }
}
