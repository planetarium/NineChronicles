using System;
using System.Collections.Generic;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.TableData.Pet;
using Nekoyume.UI.Module;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class PetEnhancementPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI petNameText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private ConditionalButton submitButton;

        [SerializeField]
        private List<GameObject> levelUpUIList;

        [SerializeField]
        private TextMeshProUGUI currentLevelText;

        [SerializeField]
        private TextMeshProUGUI targetLevelText;

        [SerializeField]
        private SweepSlider slider;

        private readonly List<IDisposable> _disposables = new();

        private const string LevelUpText = "LEVEL UP";
        private const string SummonText = "Summon";

        private int _targetLevel;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
        }

        public void ShowForSummon(PetSheet.Row petRow)
        {
            base.Show();
            levelUpUIList.ForEach(obj => obj.SetActive(false));
            titleText.text = SummonText;
            petNameText.text = $"nameof({petRow.Id})";
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{petRow.Grade}");
            contentText.text = $"contentOf({petRow.Id})";
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petRow.Id);
            petSkeletonGraphic.Initialize(true);
            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                ActionManager.Instance.PetEnhancement(petRow.Id, 1).Subscribe();
                Close();
            }).AddTo(_disposables);
        }

        public void ShowForLevelUp(PetState petState)
        {
            base.Show();
            levelUpUIList.ForEach(obj => obj.SetActive(true));
            titleText.text = LevelUpText;
            petNameText.text = $"nameof({petState.PetId})";
            gradeText.text =
                L10nManager.Localize(
                    $"UI_ITEM_GRADE_{TableSheets.Instance.PetSheet[petState.PetId].Grade}");
            contentText.text = $"contentOf({petState.PetId})";
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petState.PetId);
            petSkeletonGraphic.Initialize(true);
            currentLevelText.text = $"<size=30>Lv.</size>{petState.Level}";
            _targetLevel = petState.Level + 1;
            targetLevelText.text = _targetLevel.ToString();
            slider.Set(
                1,
                30,
                1,
                30,
                1,
                value =>
                {
                    _targetLevel = value + petState.Level;
                    targetLevelText.text = _targetLevel.ToString();
                });
            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                ActionManager.Instance.PetEnhancement(petState.PetId, _targetLevel).Subscribe();
                Close();
            }).AddTo(_disposables);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }
    }
}
