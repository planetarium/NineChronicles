using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
using Nekoyume.UI.Tween;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Subjects;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PetInventory : MonoBehaviour
    {
        public class PetDescriptionData
        {
            public int PetId;
            public int Level;
            public int CombinationSlotIndex;
            public bool Equipped;
            public bool HasState;
            public PetOptionSheet.Row.PetOptionInfo OptionInfo;
        }

        [SerializeField]
        private Transform descriptionViewParent;

        [SerializeField]
        private PetDescriptionView descriptionViewPrefab;

        private readonly Dictionary<int, PetDescriptionView> _views = new();

        public readonly Subject<int?> OnSelectedSubject = new();

        private IDisposable _disposableOnDisabled;

        private int _slotIndex;

        private void OnDisable()
        {
            _disposableOnDisabled?.Dispose();
            _disposableOnDisabled = null;
        }

        public void Initialize(bool addEmptyObject = false)
        {
            var petSheet = TableSheets.Instance.PetSheet;
            foreach (var row in petSheet)
            {
                var view = Instantiate(descriptionViewPrefab, descriptionViewParent);
                view.Initialize(row, OnSelectedSubject.OnNext);
                _views[row.Id] = view;
            }

            if (addEmptyObject)
            {
                var view = Instantiate(descriptionViewPrefab, descriptionViewParent);
                view.InitializeEmpty(OnSelectedSubject.OnNext);
                _views[default] = view;
            }
        }

        public void Toggle(int slotIndex)
        {
            if (gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                _slotIndex = slotIndex;
                Show();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateView(States.Instance.PetStates);

            if (_disposableOnDisabled != null)
            {
                _disposableOnDisabled?.Dispose();
                _disposableOnDisabled = null;
            }
            _disposableOnDisabled = States.Instance.PetStates.PetStatesSubject
                .Subscribe(UpdateView);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateView(PetStates petStates)
        {
            var viewDatas = _views.Select(x => (view: x.Value, viewData: GetViewData(x.Key)));
            var views = viewDatas
                .OrderByDescending(x => x.viewData.HasState)
                .ThenByDescending(x => x.viewData.Equipped)
                .ThenBy(x => x.viewData.CombinationSlotIndex)
                .ThenByDescending(x => x.view.Grade)
                .ThenByDescending(x => x.viewData.Level)
                .ThenBy(x => x.viewData.PetId);

            foreach (var (view, viewData) in views)
            {
                view.SetData(viewData);
                view.transform.SetAsLastSibling();
            }
        }

        private PetDescriptionData GetViewData(int petId)
        {
            var viewData = new PetDescriptionData
            {
                PetId = petId,
                CombinationSlotIndex = int.MaxValue,
            };
            var tableSheets = TableSheets.Instance;
            var petLevel = 1;

            if (States.Instance.PetStates.TryGetPetState(petId, out var petState))
            {
                petLevel = petState.Level;
                viewData.HasState = true;
            }
            else
            {
                viewData.HasState = false;
            }
            viewData.Level = petLevel;

            if (!tableSheets.PetOptionSheet.TryGetValue(petId, out var optionRow))
            {
                viewData.HasState = false;
                return viewData;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(petLevel, out var optionInfo))
            {
                viewData.HasState = false;
                return viewData;
            }
            viewData.OptionInfo = optionInfo;

            var equipped = viewData.HasState &&
                (States.Instance.PetStates.IsLocked(petId) ||
                petState.UnlockedBlockIndex > Game.Game.instance.Agent.BlockIndex);
            viewData.Equipped = equipped;

            if (equipped)
            {
                var combinationState = States.Instance.GetCombinationSlotState();
                var equippedSlot = combinationState.FirstOrDefault(x => x.Value.PetId == petId);
                viewData.CombinationSlotIndex =
                    equippedSlot.Equals(default(KeyValuePair<int, CombinationSlotState>)) ?
                    int.MaxValue : equippedSlot.Key;    
            }

            return viewData;
        }
    }
}
