using System;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RecipeRow : MonoBehaviour
    {
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
    }
}
