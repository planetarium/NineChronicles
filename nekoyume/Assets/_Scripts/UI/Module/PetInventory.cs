using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Tween;
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
            foreach (var (id, view) in _views)
            {
                if (id == default)
                {
                    continue;
                }

                view.SetData(id);
            }

            if (_views.ContainsKey(default))
            {
                var petCount = _views.Values.Count(x => x.IsAvailable);
                _views[default].transform.SetSiblingIndex(petCount - 1);
            }
        }
    }
}
