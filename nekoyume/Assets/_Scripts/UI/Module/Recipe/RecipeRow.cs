using Nekoyume.TableData;
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
            public string Name { get; private set; }

            public int Grade { get; private set; }

            public List<ItemSheet.Row> Rows { get; private set; }

            public Model(string name, int grade)
            {
                Name = name;
                Grade = grade;
                Rows = new List<ItemSheet.Row>();
            }
        }

        [SerializeField] private TextMeshProUGUI nameText = null;

        [SerializeField] private List<Image> gradeImages = null;

        [SerializeField] private List<RecipeView> recipeViews = null;

        public override void UpdateContent(Model viewModel)
        {
            nameText.text = viewModel.Name;

            for (int i = 0; i < gradeImages.Count; ++i)
            {
                gradeImages[i].enabled = i < viewModel.Grade;
            }

            for (int i = 0; i < recipeViews.Count; ++i)
            {
                if (i >= viewModel.Rows.Count)
                {
                    recipeViews[i].Hide();
                }
                else
                {
                    recipeViews[i].SetData(viewModel.Rows[i]);
                }
            }
        }
    }
}
