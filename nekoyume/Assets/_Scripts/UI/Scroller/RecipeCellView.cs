using System;
using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;
using Nekoyume.UI.Model;
using Nekoyume.Helper;
using UniRx;

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
        public MaterialIcon resultItemIcon = new MaterialIcon();
        public MaterialIcon[] materialIcons;

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
            SetIcon(resultItemIcon, recipeInfo.resultSprite, ResultIconScaleFactor);
            for (int i = 0; i < recipeInfo.materialInfos.Length; ++i)
            {
                var info = recipeInfo.materialInfos[i];
                SetIcon(materialIcons[i], info.sprite, MaterialIconScaleFactor, !info.isEnough, info.isObtained);
            }

            foreach (var info in recipeInfo.materialInfos)
            {
                if (!info.isEnough && info.sprite)
                {
                    combineButton.enabled = false;
                    combineButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                    combineText.color = ColorHelper.HexToColorRGB("92A3B5");
                    break;
                }
            }
        }

        public void SetIcon(MaterialIcon icon, Sprite sprite, float scaleFactor, bool isDimmed = false, bool isObtained = true)
        {
            if (sprite)
            {
                icon.image.transform.parent.gameObject.SetActive(true);
                if (isObtained)
                {
                    icon.image.enabled = true;
                    icon.image.overrideSprite = sprite;
                    icon.mark.enabled = false;
                }
            }
            else return;

            icon.image.SetNativeSize();
            var rect = icon.image.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * scaleFactor, rect.sizeDelta.y * scaleFactor);
            if (isDimmed)
            {
                icon.image.color = DimmedColor;
            }
        }

        public void ClearIcon(MaterialIcon icon)
        {
            icon.image.transform.parent.gameObject.SetActive(false);
            icon.image.enabled = false;
            icon.image.overrideSprite = null;
            icon.image.color = Color.white;
            icon.mark.enabled = true;
        }

        public void Clear()
        {
            ClearIcon(resultItemIcon);
            for (int i = 0; i < materialIcons.Length; ++i)
            {
                ClearIcon(materialIcons[i]);
                combineButton.enabled = true;
                combineButton.image.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
                combineText.color = Color.white;
            }
        }
    }
}
