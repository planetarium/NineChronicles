using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class RecipeCellView : EnhancedScrollerCellView
    {
        public GameObject obj;
        public Text resultNameText;
        public Image resultItemIcon;
        public Text resultMark;
        public Image[] materialIcons;
        public Text[] materialMarks;

        private const float ResultIconScaleFactor = 1.2f;
        private const float MaterialIconScaleFactor = 1.2f;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
        }

        #endregion

        public void SetData(RecipeInfo recipe)
        {
            obj.SetActive(true);
            resultNameText.text = recipe.resultName;
            SetIcon(resultItemIcon, recipe.resultSprite, resultMark, ResultIconScaleFactor);
            for(int i = 0; i < 4; ++i)
            {
                SetIcon(materialIcons[i], recipe.materialSprites[i], materialMarks[i], MaterialIconScaleFactor);
            }
        }

        public void SetIcon(Image image, Sprite sprite, Text text, float scaleFactor = 1f)
        {
            if (sprite)
            {
                image.transform.parent.gameObject.SetActive(true);
                image.enabled = true;
                text.enabled = false;
                image.overrideSprite = sprite;
            }
            else return;

            image.SetNativeSize();
            var rect = image.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * scaleFactor, rect.sizeDelta.y * scaleFactor);
        }
    }
}
