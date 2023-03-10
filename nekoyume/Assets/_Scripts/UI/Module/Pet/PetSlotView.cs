using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class PetSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image petImage;

        [SerializeField]
        private Image soulStoneImage;

        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private GameObject petInfoObject;

        [SerializeField]
        private TextMeshProUGUI soulStoneText;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI petInfoText;

        [SerializeField]
        private GameObject equippedIcon;

        [SerializeField]
        private GameObject levelUpNotification;

        [SerializeField]
        private GameObject hasNotification;

        [SerializeField]
        private GameObject loading;

        [SerializeField]
        private GameObject summonableNotification;

        private PetState _petState;
        private readonly List<IDisposable> _disposables = new();

        public void Set(PetSlotViewModel model, PetSlotScroll.ContextModel context)
        {
            _disposables.DisposeAllAndClear();
            model ??= new PetSlotViewModel();

            button.OnClickAsObservable()
                .Select(_ => model)
                .Subscribe(context.OnClick.OnNext)
                .AddTo(_disposables);
            emptyObject.SetActive(model.Empty.Value);
            petInfoObject.SetActive(!model.Empty.Value);
            equippedIcon.SetActive(false);
            hasNotification.SetActive(false);
            levelUpNotification.SetActive(false);
            loading.SetActive(false);
            summonableNotification.SetActive(false);
            if (model.PetRow is null)
            {
                return;
            }

            var isOwn = States.Instance.PetStates.TryGetPetState(model.PetRow.Id, out _petState);
            petImage.overrideSprite = PetFrontHelper.GetPetCardSprite(model.PetRow.Id);
            soulStoneImage.overrideSprite = PetFrontHelper.GetSoulStoneSprite(model.PetRow.Id);
            soulStoneText.text = States.Instance.AvatarBalance[model.PetRow.SoulStoneTicker]
                .GetQuantityString();
            petImage.color = isOwn
                ? Color.white
                : PetFrontHelper.GetUIColor(PetFrontHelper.NotOwnSlot);
            var maxLevel = TableSheets.Instance.PetCostSheet[model.PetRow.Id]
                .Cost
                .OrderBy(data => data.Level)
                .Last()
                .Level;
            levelText.text = isOwn
                ? $"<size=14>Lv.</size>{_petState.Level}/{maxLevel}"
                : L10nManager.Localize("UI_NOT_POSSESSED");
            levelText.color = isOwn
                ? _petState.Level == maxLevel
                    ? PetFrontHelper.GetUIColor(PetFrontHelper.MaxLevelText)
                    : Color.white
                : PetFrontHelper.GetUIColor(PetFrontHelper.NotOwnText);
            petInfoText.color = Color.white;
            petInfoText.text = L10nManager.Localize($"PET_NAME_{model.PetRow.Id}");
            model.EquippedIcon.SubscribeTo(equippedIcon).AddTo(_disposables);
            model.HasNotification.Subscribe(b =>
            {
                var levelUpAble = b && _petState is not null;
                var summonAble = b && _petState is null;
                hasNotification.SetActive(b);
                levelUpNotification.SetActive(levelUpAble);
                summonableNotification.SetActive(summonAble);
                if (summonAble)
                {
                    petInfoText.color =
                        PetFrontHelper.GetUIColor(PetFrontHelper.SummonableText);
                    petInfoText.text = L10nManager.Localize("UI_SUMMONABLE");
                }
            }).AddTo(_disposables);
            LoadingHelper.PetEnhancement
                .Subscribe(id =>
                {
                    var isLoading = id == model.PetRow.Id;
                    loading.SetActive(isLoading);
                    if (isLoading)
                    {
                        petInfoText.color =
                            PetFrontHelper.GetUIColor(PetFrontHelper.LevelUpText);
                        petInfoText.text =
                            L10nManager.Localize(isOwn
                                ? "UI_LEVELUP_IN_PROGRESS"
                                : "UI_SUMMONING_IN_PROGRESS");
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
