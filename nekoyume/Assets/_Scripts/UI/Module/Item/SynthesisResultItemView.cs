#nullable enable

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

    public class SynthesisResultItemView : VanillaItemView
    {
        [Space]
        [SerializeField] private Animator animator = null!;

        [SerializeField] private TouchHandler touchHandler = null!;
        [SerializeField] private CanvasGroup canvasGroup = null!;

        [SerializeField] private TextMeshProUGUI countText = null!;
        [SerializeField] private ItemOptionTag optionTag = null!;

        [SerializeField] private TextMeshProUGUI nameText = null!;
        [SerializeField] private TextMeshProUGUI infoText = null!;
        [SerializeField] private GameObject successObject = null!;

        private readonly List<IDisposable> _disposables = new();
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");

        public void SetData(ItemBase itemBase, bool hideWithAlpha = false, bool showDetail = false)
        {
            base.SetData(itemBase);

            if (itemBase is not IEquippableItem)
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

            countText.gameObject.SetActive(false);
            optionTag.Set(itemBase);
            optionTag.gameObject.SetActive(true);

            nameText.gameObject.SetActive(showDetail);

            if (!showDetail)
            {
                return;
            }

            nameText.text = itemBase.GetLocalizedName(false);
            infoText.text = itemBase.GetLocalizedInformation();
        }

        public void ShowWithAnimation()
        {
            canvasGroup.alpha = 1;
            animator.SetTrigger(AnimatorHashShow);
        }

        public void SetSuccess(bool success)
        {
            successObject.SetActive(success);
        }
    }
}
