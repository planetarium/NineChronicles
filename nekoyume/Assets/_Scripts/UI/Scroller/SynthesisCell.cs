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

    public class SynthesisCell : MonoBehaviour
    {
        [SerializeField] private GameObject activeBackgroundObject = null!;
        [SerializeField] private GameObject inactiveBackgroundObject = null!;

        [SerializeField] private TMP_Text nameText = null!;
        [SerializeField] private TMP_Text holdText = null!;
        [SerializeField] private ConditionalButton selectButton = null!;

        public SynthesizeModel? SynthesizeModel { get; private set; }

        public bool IsInitialized => SynthesizeModel != null;

        private Grade _grade = Grade.Normal;

        private void Awake()
        {
            CheckNull();
        }

        public void Initialize(Grade grade)
        {
            _grade = grade;
            SetTitleText();
            selectButton.OnClickSubject
                .Subscribe(_ =>
                {
                    var synthesisWidget = Widget.Find<Synthesis>();
                    synthesisWidget.OnClickGradeItem(SynthesizeModel);
                })
                .AddTo(gameObject);
        }

        public void UpdateContent(SynthesizeModel synthesizeModel)
        {
            SynthesizeModel = synthesizeModel;
            SetData(synthesizeModel.InventoryItemCount, synthesizeModel.RequiredItemCount);
        }

        // 모든 셀이 인벤토리 정보를 불러와서 처리하지 않도록 해당 셀 스크립트가 아닌 상위 오브젝트 스크립트에서 계산
        private void SetData(int inventoryItemCount, int requiredItemCount)
        {
            if (requiredItemCount == 0)
            {
                throw new ArgumentException("requiredItemCount is 0");
            }

            var header = L10nManager.Localize("UI_SYNTHESIZE_HOLDS")!;
            var outputItemCount = inventoryItemCount / requiredItemCount;
            holdText.text = $"{header}: {inventoryItemCount}/{requiredItemCount} ({outputItemCount})";

            if (outputItemCount > 0)
            {
                activeBackgroundObject.SetActive(true);
                inactiveBackgroundObject.SetActive(false);
                selectButton.SetCondition(() => true);
            }
            else
            {
                activeBackgroundObject.SetActive(false);
                inactiveBackgroundObject.SetActive(true);
                selectButton.SetCondition(() => false);
            }

            selectButton.UpdateObjects();
        }

        #region Utils

        private void SetTitleText()
        {
            nameText.text = L10nManager.Localize("UI_SYNTHESIZE_MATERIAL", GetGradeText(_grade));
            nameText.color = LocalizationExtensions.GetItemGradeColor((int)_grade);
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
