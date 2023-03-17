using System;
using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module.Pet;
using Nekoyume.UI.Scroller;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class PetSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private PetInfoView infoView;

        [SerializeField]
        private SkeletonGraphic petGraphic;

        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private GameObject petInfoObject;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private GameObject levelUpNotification;

        [SerializeField]
        private GameObject hasNotification;

        [SerializeField]
        private GameObject dimObject;

        [SerializeField]
        private GameObject summonableNotification;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private List<UIHsvModifier> uiHsvModifiers;

        [SerializeField]
        private GameObject selectedObject;

        [SerializeField]
        private TextMeshProUGUI stateText;

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
            selectedObject.SetActive(false);
            dimObject.SetActive(false);
            loadingObject.SetActive(false);
            summonableNotification.SetActive(false);
            if (model.PetRow is null)
            {
                return;
            }

            petGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(model.PetRow.Id);
            petGraphic.rectTransform.localPosition =
                PetFrontHelper.GetLocalPositionInCard(model.PetRow.Id);
            petGraphic.rectTransform.localScale =
                PetFrontHelper.GetLocalScaleInCard(model.PetRow.Id);
            petGraphic.Initialize(true);
            var hsv = PetFrontHelper.GetHsv(model.PetRow.Id);
            uiHsvModifiers.ForEach(modifier =>
            {
                modifier.hue = hsv.x;
                modifier.saturation = hsv.y;
                modifier.value = hsv.z;
            });
            var maxLevel = TableSheets.Instance.PetCostSheet[model.PetRow.Id]
                .Cost
                .OrderBy(data => data.Level)
                .Last()
                .Level;
            var isOwn = States.Instance.PetStates.TryGetPetState(model.PetRow.Id, out _petState);
            var isMaxLevel = _petState?.Level == maxLevel;
            infoView.Set(model.PetRow.Id, model.PetRow.Grade);
            levelText.text = isOwn
                ? $"Lv.{_petState.Level}"
                : "-";
            levelText.color = isMaxLevel
                ? PetFrontHelper.GetUIColor(PetFrontHelper.MaxLevelText)
                : Color.white;
            dimObject.SetActive(!isOwn);

            model.HasNotification.Subscribe(b =>
            {
                var levelUpAble = b && _petState is not null;
                var summonAble = b && _petState is null;
                hasNotification.SetActive(b);
                levelUpNotification.SetActive(levelUpAble);
                summonableNotification.SetActive(summonAble);
            }).AddTo(_disposables);
            model.Empty.Subscribe(b =>
            {
                emptyObject.SetActive(b);
                petInfoObject.SetActive(!b);
            }).AddTo(_disposables);
            model.Selected.SubscribeTo(selectedObject).AddTo(_disposables);
            LoadingHelper.PetEnhancement.Subscribe(id =>
            {
                var isLoading = id == model.PetRow.Id;
                loadingObject.SetActive(isLoading);
                if (isLoading)
                {
                    dimObject.SetActive(true);
                    summonableNotification.SetActive(false);
                    stateText.text =
                        L10nManager.Localize(isOwn
                            ? "UI_LEVELUP_IN_PROGRESS"
                            : "UI_SUMMONING_IN_PROGRESS");
                }
            }).AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
