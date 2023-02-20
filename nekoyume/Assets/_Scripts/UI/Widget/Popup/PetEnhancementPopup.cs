using System;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField]
        private Image requiredSoulStoneImage;

        [SerializeField]
        private TextMeshProUGUI soulStoneCostText;

        [SerializeField]
        private TextMeshProUGUI ncgCostText;

        [SerializeField]
        private GameObject ncgCostObject;

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
            petNameText.text = L10nManager.Localize($"PET_NAME_{petRow.Id}");
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{petRow.Grade}");
            var option = TableSheets.Instance.PetOptionSheet[petRow.Id].LevelOptionMap[1];
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petRow.Id);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetRenderingHelper.GetSoulStoneSprite(petRow.Id);
            submitButton.OnSubmitSubject.Subscribe(_ =>
            {
                Action(petRow.Id, 1);
            }).AddTo(_disposables);
            TableSheets.Instance.PetCostSheet[petRow.Id].TryGetCost(1, out var cost);
            SetCost(cost.NcgQuantity, cost.SoulStoneQuantity);
        }

        public void ShowForLevelUp(PetState petState)
        {
            base.Show();
            levelUpUIList.ForEach(obj => obj.SetActive(true));
            titleText.text = LevelUpText;
            petNameText.text = L10nManager.Localize($"PET_NAME_{petState.PetId}");
            gradeText.text =
                L10nManager.Localize(
                    $"UI_ITEM_GRADE_{TableSheets.Instance.PetSheet[petState.PetId].Grade}");
            var option = TableSheets.Instance.PetOptionSheet[petState.PetId].LevelOptionMap[petState.Level];
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
            petSkeletonGraphic.skeletonDataAsset =
                PetRenderingHelper.GetPetSkeletonData(petState.PetId);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetRenderingHelper.GetSoulStoneSprite(petState.PetId);
            currentLevelText.text = $"<size=30>Lv.</size>{petState.Level}";
            var maxLevel = TableSheets.Instance.PetCostSheet[petState.PetId].Cost
                .OrderBy(data => data.Level)
                .Last()
                .Level;
            if (petState.Level < maxLevel)
            {
                _targetLevel = petState.Level + 1;
                targetLevelText.text = _targetLevel.ToString();
                option = TableSheets.Instance.PetOptionSheet[petState.PetId].LevelOptionMap[_targetLevel];
                contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
                slider.Set(
                    1,
                    maxLevel - petState.Level,
                    1,
                    maxLevel - petState.Level,
                    1,
                    value =>
                    {
                        _targetLevel = value + petState.Level;
                        targetLevelText.text = _targetLevel.ToString();
                        var (ncg, soulStone) = PetHelper.CalculateEnhancementCost(
                            TableSheets.Instance.PetCostSheet,
                            petState.PetId,
                            petState.Level,
                            _targetLevel);
                        SetCost(ncg, soulStone);
                        option = TableSheets.Instance.PetOptionSheet[petState.PetId].LevelOptionMap[_targetLevel];
                        contentText.text =
                            L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",
                                option.OptionValue);
                    });
                submitButton.OnSubmitSubject.Subscribe(_ =>
                {
                    Action(petState.PetId, _targetLevel);
                }).AddTo(_disposables);
            }
            else
            {
                levelUpUIList.ForEach(obj => obj.SetActive(false));
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void SetCost(int ncg, int soulStone)
        {
            ncgCostText.text = ncg.ToString();
            soulStoneCostText.text = soulStone.ToString();
            ncgCostObject.SetActive(ncg > 0);
        }
        private void Action(int petId, int targetLevel)
        {
            ActionManager.Instance.PetEnhancement(petId, targetLevel).Subscribe();
            LoadingHelper.PetEnhancement.Value = petId;
            Close();
        }
    }
}
