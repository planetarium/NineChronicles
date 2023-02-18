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

namespace Nekoyume.UI.Module.Pet
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
            petImage.overrideSprite = PetRenderingHelper.GetPetCardSprite(model.PetRow.Id);
            soulStoneImage.overrideSprite = PetRenderingHelper.GetSoulStoneSprite(model.PetRow.Id);
            soulStoneText.text = States.Instance.AvatarBalance[model.PetRow.SoulStoneTicker]
                .GetQuantityString();
            petImage.color = isOwn
                ? Color.white
                : PetRenderingHelper.GetUIColor(PetRenderingHelper.NotOwnSlot);
            petInfoText.text = $"{model.PetRow.Id}.Localize";
            var maxLevel = TableSheets.Instance.PetCostSheet[model.PetRow.Id]
                .OrderedCostList
                .Last()
                .Level;
            levelText.text = isOwn
                ? $"<size=14>Lv.</size>{_petState.Level}/{maxLevel}"
                : L10nManager.Localize("UI_NOT_POSSESSED");
            levelText.color = isOwn
                ? Color.white
                : PetRenderingHelper.GetUIColor(PetRenderingHelper.NotOwnText);
            model.EquippedIcon.SubscribeTo(equippedIcon).AddTo(_disposables);
            model.HasNotification.Subscribe(b =>
            {
                hasNotification.SetActive(b);
                levelUpNotification.SetActive(b && _petState is not null);
                summonableNotification.SetActive(b && _petState is null);
            }).AddTo(_disposables);
            model.Loading.SubscribeTo(loading).AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
