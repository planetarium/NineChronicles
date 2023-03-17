using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
using System;
using System.Collections.Generic;
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
            public bool Empty;
            public int PetId;
            public int Level;
            public int CombinationSlotIndex;
            public bool Equipped;
            public bool HasState;
            public PetOptionSheet.Row.PetOptionInfo OptionInfo;
            public string Description;
            public bool IsAppliable;
        }

        [SerializeField]
        private Transform descriptionViewParent;

        [SerializeField]
        private PetDescriptionView descriptionViewPrefab;

        [SerializeField]
        private ScrollRect scrollRect;

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
            UpdateView(States.Instance.PetStates);

            if (_disposableOnDisabled != null)
            {
                _disposableOnDisabled?.Dispose();
                _disposableOnDisabled = null;
            }
            _disposableOnDisabled = States.Instance.PetStates.PetStatesSubject
                .Subscribe(state => UpdateView(state));
            gameObject.SetActive(true);
            InitScrollPosition();
        }

        public void Show(Craft.CraftInfo craftInfo)
        {
            if (craftInfo.Equals(default))
            {
                Show();
                return;
            }

            UpdateView(States.Instance.PetStates, craftInfo);

            if (_disposableOnDisabled != null)
            {
                _disposableOnDisabled?.Dispose();
                _disposableOnDisabled = null;
            }
            _disposableOnDisabled = States.Instance.PetStates.PetStatesSubject
                .Subscribe(state => UpdateView(state, craftInfo));
            gameObject.SetActive(true);
            InitScrollPosition();
        }

        public void InitScrollPosition()
        {
            var posX = scrollRect.content.anchoredPosition.x;
            scrollRect.content.anchoredPosition = new Vector3(posX, 0);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateView(PetStates petStates, Craft.CraftInfo? craftInfo = null)
        {
            var viewDatas = _views.Select(x =>
                (view: x.Value, viewData: GetViewData(x.Key, petStates, craftInfo)));
            var views = viewDatas
                .OrderBy(x => x.viewData.Equipped)
                .ThenByDescending(x => x.viewData.IsAppliable)
                .ThenByDescending(x => x.viewData.Empty)
                .ThenByDescending(x => x.viewData.HasState)
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

        private PetDescriptionData GetViewData(int petId, PetStates petStates, Craft.CraftInfo? craftInfo)
        {
            var viewData = new PetDescriptionData
            {
                PetId = petId,
                CombinationSlotIndex = int.MaxValue,
            };
            var tableSheets = TableSheets.Instance;
            var petLevel = 1;

            if (petStates.TryGetPetState(petId, out var petState))
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
                viewData.Empty = true;
                return viewData;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(petLevel, out var optionInfo))
            {
                viewData.HasState = false;
                viewData.Empty = true;
                return viewData;
            }
            viewData.OptionInfo = optionInfo;

            var equipped = viewData.HasState &&
                (petStates.IsLocked(petId) ||
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

            if (craftInfo.HasValue && viewData.HasState)
            {
                (var description, var applied) = PetFrontHelper.GetDescriptionText(
                    optionInfo,
                    craftInfo.Value,
                    petState,
                    States.Instance.GameConfigState);
                viewData.Description = description;
                viewData.IsAppliable = applied;
            }
            else
            {
                viewData.Description = L10nManager.Localize(
                    $"PET_DESCRIPTION_{optionInfo.OptionType}",
                    optionInfo.OptionValue);
                viewData.IsAppliable = true;
            }

            return viewData;
        }
    }
}
