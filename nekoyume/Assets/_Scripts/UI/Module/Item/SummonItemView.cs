using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SummonItemView : VanillaItemView
    {
        [Space]
        [SerializeField] private Animator animator;
        [SerializeField] private TouchHandler touchHandler;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject grade5Effect;
        [SerializeField] private GameObject grade4Effect;

        [SerializeField] private ItemOptionTag optionTag;

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI infoText;

        private readonly List<IDisposable> _disposables = new();
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashHide = Animator.StringToHash("Normal");

        public void SetData(ItemBase itemBase, bool hideWithAlpha = false, bool showDetail = false)
        {
            base.SetData(itemBase);

            if (itemBase is not Equipment equipment)
            {
                return;
            }

            _disposables.DisposeAllAndClear();
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                var tooltip = ItemTooltip.Find(ItemType.Equipment);
                tooltip.Show(itemBase, string.Empty, false, null);
            }).AddTo(_disposables);

            if (hideWithAlpha)
            {
                canvasGroup.alpha = 0;
            }

            grade5Effect.SetActive(equipment.Grade == 5);
            grade4Effect.SetActive(equipment.Grade == 4);

            optionTag.Set(equipment);

            nameText.gameObject.SetActive(showDetail);
            if (showDetail)
            {
                nameText.text = equipment.GetLocalizedName(false);
                infoText.text = equipment.GetLocalizedInformation();
            }
        }

        public void ShowWithAnimation()
        {
            canvasGroup.alpha = 1;
            animator.SetTrigger(AnimatorHashShow);
        }
    }
}
