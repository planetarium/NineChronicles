using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Tween;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                UpdateView(slotIndex);
                Show();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);

            foreach (var (id, view) in _views)
            {
                if (!States.Instance.PetStates.TryGetPetState(id, out var state))
                {
                    view.Hide();
                    continue;
                }

                view.SetData(state, false);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateView(int slotIndex)
        {

        }
    }
}
