using System;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RecipeRow : RectCell<
        RecipeRow.Model,
        RecipeScroll.ContextModel>
    {
        public class Model
        {
            public ItemSubType ItemSubType { get; set; }

            public StatType StatType { get; set; }

            public string Name { get; }

            public int Grade { get; }

            public List<SheetRow<int>> Rows { get; }

            public Model(string name, int grade)
            {
                Name = name;
                Grade = grade;
                Rows = new List<SheetRow<int>>();
            }
        }

        [Serializable]
        private struct TitleContent
        {
            public TextMeshProUGUI nameText;
            public Image[] gradeImages;
        }

        [SerializeField]
        private TitleContent normalGradeTitleContent;

        [SerializeField]
        private TitleContent legendaryGradeTitleContent;

        [SerializeField]
        private List<RecipeCell> recipeCells;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private UIHsvModifier gradeEffectObject;

        [SerializeField]
        private Sprite equipmentGradeSprite;

        [SerializeField]
        private Sprite consumableGradeSprite;

        private readonly int _triggerHash = Animator.StringToHash("Show");

        public override void UpdateContent(Model viewModel)
        {
            var isNormalGrade = viewModel.Grade < 5 || viewModel.ItemSubType == ItemSubType.Food;
            normalGradeTitleContent.nameText.gameObject.SetActive(isNormalGrade);
            legendaryGradeTitleContent.nameText.gameObject.SetActive(!isNormalGrade);
            var titleContent = isNormalGrade
                ? normalGradeTitleContent
                : legendaryGradeTitleContent;

            titleContent.nameText.text = viewModel.Name;
            for (var i = 0; i < titleContent.gradeImages.Length; ++i)
            {
                var image = titleContent.gradeImages[i];

                if (i < viewModel.Grade)
                {
                    image.enabled = true;
                    image.sprite =
                        viewModel.ItemSubType == ItemSubType.Food ?
                        consumableGradeSprite :
                        equipmentGradeSprite;
                }
                else
                {
                    image.enabled = false;
                }
            }
            gradeEffectObject.enabled = !isNormalGrade;

            for (var i = 0; i < recipeCells.Count; ++i)
            {
                if (i >= viewModel.Rows.Count)
                {
                    recipeCells[i].Hide();
                }
                else
                {
                    var cell = recipeCells[i];
                    cell.Show(viewModel.Rows[i]);
                }
            }
        }

        public void HideWithAlpha()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            canvasGroup.alpha = 0f;
        }

        public void ShowWithAlpha(bool ignoreShowAnimation = false)
        {
            if (!gameObject.activeSelf && !ignoreShowAnimation)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            if (ignoreShowAnimation)
            {
                return;
            }

            animator.SetTrigger(_triggerHash);
        }
    }
}
