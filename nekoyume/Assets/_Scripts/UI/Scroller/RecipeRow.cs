using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using System.Collections.Generic;
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

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private List<Image> gradeImages;

        [SerializeField]
        private List<RecipeCell> recipeCells;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Sprite equipmentGradeSprite;

        [SerializeField]
        private Sprite consumableGradeSprite;

        private readonly int _triggerHash = Animator.StringToHash("Show");

        public override void UpdateContent(Model viewModel)
        {
            nameText.text = viewModel.Name;

            for (var i = 0; i < gradeImages.Count; ++i)
            {
                var image = gradeImages[i];

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

        public void ShowAnimation()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            animator.SetTrigger(_triggerHash);
        }
    }
}
