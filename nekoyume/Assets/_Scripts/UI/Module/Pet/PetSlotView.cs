using System;
using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Pet
{
    using UniRx;
    public class PetSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private SkeletonGraphic petGraphic;

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
        private GameObject levelUpNotification;

        [SerializeField]
        private GameObject hasNotification;

        [SerializeField]
        private GameObject loading;

        [SerializeField]
        private GameObject summonableNotification;

        [SerializeField]
        private List<UIHsvModifier> uiHsvModifiers;

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
            hasNotification.SetActive(false);
            levelUpNotification.SetActive(false);
            loading.SetActive(false);
            summonableNotification.SetActive(false);
            if (model.PetRow is null)
            {
                return;
            }

            var isOwn = States.Instance.PetStates.TryGetPetState(model.PetRow.Id, out _petState);
            petGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(model.PetRow.Id);
            petGraphic.rectTransform.localPosition =
                PetRenderingHelper.GetLocalPositionInCard(model.PetRow.Id);
            petGraphic.rectTransform.localScale =
                PetRenderingHelper.GetLocalScaleInCard(model.PetRow.Id);
            petGraphic.Initialize(true);
            var hsv = PetRenderingHelper.GetHsv(model.PetRow.Id);
            uiHsvModifiers.ForEach(modifier =>
            {
                modifier.hue = hsv.x;
                modifier.saturation = hsv.y;
                modifier.value = hsv.z;
            });
            soulStoneImage.overrideSprite = PetRenderingHelper.GetSoulStoneSprite(model.PetRow.Id);
            soulStoneText.text = States.Instance.AvatarBalance[model.PetRow.SoulStoneTicker]
                .GetQuantityString();
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
                    ? PetRenderingHelper.GetUIColor(PetRenderingHelper.MaxLevelText)
                    : Color.white
                : PetRenderingHelper.GetUIColor(PetRenderingHelper.NotOwnText);
            petInfoText.color = Color.white;
            petInfoText.text = L10nManager.Localize($"PET_NAME_{model.PetRow.Id}");
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
                        PetRenderingHelper.GetUIColor(PetRenderingHelper.SummonableText);
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
                            PetRenderingHelper.GetUIColor(PetRenderingHelper.LevelUpText);
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
