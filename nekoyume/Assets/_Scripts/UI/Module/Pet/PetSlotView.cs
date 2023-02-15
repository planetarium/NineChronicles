using System;
using System.Collections.Generic;
using Nekoyume.Model.State;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module.Pet
{
    public class PetSlotView : MonoBehaviour
    {
        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private GameObject petInfoObject;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI petInfoText;

        [SerializeField]
        private GameObject equippedIcon;

        [SerializeField]
        private GameObject hasNotification;

        [SerializeField]
        private GameObject loading;

        private PetState _petState;
        private readonly List<IDisposable> _disposables = new();

        public void Set(PetSlotViewModel model)
        {
            _disposables.DisposeAllAndClear();

            emptyObject.SetActive(model.Empty.Value);
            petInfoObject.SetActive(!model.Empty.Value);
            if (model.PetRow is null)
            {
                return;
            }

            petInfoText.text = $"{model.PetRow.Id}.Localize";
            levelText.text =
                States.Instance.PetStates.TryGetPetState(model.PetRow.Id, out _petState)
                    ? $"<size=14>Lv.</size>{_petState.Level}/Max"
                    : "not own";
            model.EquippedIcon.SubscribeTo(equippedIcon).AddTo(_disposables);
            model.HasNotification.SubscribeTo(hasNotification).AddTo(_disposables);
            model.Loading.SubscribeTo(loading).AddTo(_disposables);
        }
    }
}
