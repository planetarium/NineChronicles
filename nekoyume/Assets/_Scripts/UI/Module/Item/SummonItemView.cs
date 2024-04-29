using System;
using System.Collections.Generic;
using Libplanet.Types.Assets;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
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
        [SerializeField] private GameObject grade6Effect;
        [SerializeField] private GameObject grade5Effect;
        [SerializeField] private GameObject grade4Effect;
        [SerializeField] private GameObject gradeEffect;

        [SerializeField] private TextMeshProUGUI countText;
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

            grade6Effect.SetActive(equipment.Grade == 6);
            grade5Effect.SetActive(equipment.Grade == 5);
            grade4Effect.SetActive(equipment.Grade == 4);
            gradeEffect.SetActive(false);

            countText.gameObject.SetActive(false);
            optionTag.Set(equipment);
            optionTag.gameObject.SetActive(true);

            nameText.gameObject.SetActive(showDetail);
            if (showDetail)
            {
                nameText.text = equipment.GetLocalizedName(false);
                infoText.text = equipment.GetLocalizedInformation();
            }
        }

        public void SetData(FungibleAssetValue fav, bool hideWithAlpha = false, bool showDetail = false)
        {
            base.SetData(fav);

            _disposables.DisposeAllAndClear();
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                var tooltip = Widget.Find<FungibleAssetTooltip>();
                tooltip.Show(fav, null);
            }).AddTo(_disposables);

            if (hideWithAlpha)
            {
                canvasGroup.alpha = 0;
            }

            var grade = Util.GetTickerGrade(fav.Currency.Ticker);
            grade6Effect.SetActive(grade == 6);
            grade5Effect.SetActive(grade == 5);
            grade4Effect.SetActive(grade == 4);
            gradeEffect.SetActive(false);

            countText.text = fav.GetQuantityString();
            countText.gameObject.SetActive(true);
            optionTag.gameObject.SetActive(false);

            nameText.gameObject.SetActive(showDetail);
            if (showDetail)
            {
                nameText.text = fav.GetLocalizedName();
                infoText.text = fav.GetLocalizedInformation();
            }
        }

        public void ShowWithAnimation()
        {
            canvasGroup.alpha = 1;
            animator.SetTrigger(AnimatorHashShow);

            if (grade4Effect.activeSelf || grade5Effect.activeSelf || grade6Effect.activeSelf)
            {
                gradeEffect.SetActive(true);
            }
        }
    }
}
