#nullable enable

using System;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class SynthesisCell : RectCell<SynthesizeModel, SynthesisScroll.ContextModel>
    {
        [SerializeField] Grade grade = Grade.Normal;

        [SerializeField] private GameObject activeBackgroundObject = null!;
        [SerializeField] private GameObject inactiveBackgroundObject = null!;

        [SerializeField] private TMP_Text nameText = null!;
        [SerializeField] private TMP_Text holdText = null!;
        [SerializeField] private ConditionalButton selectButton = null!;

        private void Awake()
        {
            CheckNull();

            SetTitleText();
            selectButton.OnSubmitSubject
                .Subscribe(_ => Context.OnClickSelectButton.OnNext(grade))
                .AddTo(gameObject);
        }

        public override void UpdateContent(SynthesizeModel synthesizeModel)
        {
            SetData(synthesizeModel.InventoryItemCount, synthesizeModel.NeedItemCount);
        }

        // 모든 셀이 인벤토리 정보를 불러와서 처리하지 않도록 해당 셀 스크립트가 아닌 상위 오브젝트 스크립트에서 계산
        public void SetData(int inventoryItemCount, int needItemCount)
        {
            var header = L10nManager.Localize("UI_SYNTHESIZE_HOLDS")!;
            var outputItemCount = inventoryItemCount / needItemCount;
            holdText.text = $"{header}: {inventoryItemCount}/{needItemCount} ({outputItemCount})";

            if (outputItemCount > 0)
            {
                activeBackgroundObject.SetActive(true);
                inactiveBackgroundObject.SetActive(false);
                selectButton.SetCondition(() => false);
            }
            else
            {
                activeBackgroundObject.SetActive(false);
                inactiveBackgroundObject.SetActive(true);
                selectButton.SetCondition(() => true);
            }

            selectButton.UpdateObjects();
        }

        #region Utils

        private void SetTitleText()
        {
            nameText.text = L10nManager.Localize("UI_SYNTHESIZE_MATERIAL", GetGradeText(grade));
            nameText.color = LocalizationExtensions.GetItemGradeColor((int)grade);
        }

        private string GetGradeText(Grade targetGrade) => GetGradeText((int)targetGrade);

        private string GetGradeText(int targetGrade) => L10nManager.Localize($"UI_ITEM_GRADE_{targetGrade}");

        private void CheckNull()
        {
            if (activeBackgroundObject == null)
            {
                throw new NullReferenceException("activeBackgroundObject is null");
            }

            if (inactiveBackgroundObject == null)
            {
                throw new NullReferenceException("inactiveBackgroundObject is null");
            }

            if (nameText == null)
            {
                throw new NullReferenceException("nameText is null");
            }

            if (holdText == null)
            {
                throw new NullReferenceException("holdText is null");
            }

            if (selectButton == null)
            {
                throw new NullReferenceException("selectButton is null");
            }
        }

        #endregion Utils
    }
}
