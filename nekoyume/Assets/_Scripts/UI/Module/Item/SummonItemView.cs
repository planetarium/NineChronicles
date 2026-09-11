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
    using Coffee.UIExtensions;
    using UniRx;

    public class SummonItemView : VanillaItemView
    {
        [Space]
        [SerializeField] private Animator animator;

        [SerializeField] private TouchHandler touchHandler;
        [SerializeField] private CanvasGroup canvasGroup;
        /// <summary>프리팹이 가진 가장 높은 등급 연출. 이 위 등급은 이걸 쓴다.</summary>
        private const int MaxEffectGrade = 8;

        /// <summary>아이템은 4등급부터 연출이 붙는다(프리팹에 4~8 이 있다).</summary>
        private const int ItemLowestEffectGrade = 4;

        /// <summary>룬(FAV)은 7등급부터만 붙인다 — 기존 동작.</summary>
        private const int FavLowestEffectGrade = 7;

        [SerializeField] private GameObject grade8Effect;
        [SerializeField] private GameObject grade7Effect;
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

            SetGradeEffects(itemBase.Grade, ItemLowestEffectGrade);

            countText.gameObject.SetActive(false);
            optionTag.Set(itemBase);
            optionTag.gameObject.SetActive(true);

            nameText.gameObject.SetActive(showDetail);
            if (showDetail)
            {
                nameText.text = itemBase.GetLocalizedName(false);
                infoText.text = itemBase.GetLocalizedInformation();
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

            SetGradeEffects(
                Util.GetTickerGrade(fav.Currency.Ticker), FavLowestEffectGrade);

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

        /// <summary>
        /// 등급 연출을 켠다. 프리팹에 있는 최상위 등급(<see cref="MaxEffectGrade"/>)보다 높은
        /// 등급은 그 최상위 연출을 쓴다.
        /// </summary>
        /// <param name="grade">아이템/FAV 등급.</param>
        /// <param name="lowestEffectGrade">이 등급 미만은 연출을 켜지 않는다.</param>
        /// <remarks>
        /// 등급마다 <c>SetActive(grade == N)</c> 한 줄을 더하는 구조였다. 그래서 새 등급이
        /// 들어오면 <b>아무 연출도 붙지 않았다</b> — 등급 9 "Ultimate" 가 실제로 그랬다.
        /// 상한으로 잘라 두면 등급이 늘어도 최상위 연출이 따라온다. 이펙트를 상위 등급으로
        /// 취급하는 건 <c>UI_ItemViewData</c> 의 파티클·강화 재질과 같은 관례다
        /// (배색처럼 1~8 을 재사용하는 주기를 돌리지 않는다).
        /// </remarks>
        private void SetGradeEffects(int grade, int lowestEffectGrade)
        {
            var effectGrade = Mathf.Min(grade, MaxEffectGrade);
            if (effectGrade < lowestEffectGrade)
            {
                effectGrade = 0;
            }

            grade8Effect.SetActive(effectGrade == 8);
            grade7Effect.SetActive(effectGrade == 7);
            grade6Effect.SetActive(effectGrade == 6);
            grade5Effect.SetActive(effectGrade == 5);
            grade4Effect.SetActive(effectGrade == 4);
            gradeEffect.SetActive(false);
        }

        public void ShowWithAnimation()
        {
            canvasGroup.alpha = 1;
            animator.SetTrigger(AnimatorHashShow);

            if (grade7Effect.activeSelf || grade8Effect.activeSelf)
            {
                var effect = gradeEffect.GetComponent<UIParticle>();
                if (grade7Effect.activeSelf)
                {
                    effect.scale = 240;
                }
                else if (grade8Effect.activeSelf)
                {
                    effect.scale = 320;
                }
                gradeEffect.SetActive(true);
            }
        }
    }
}
