using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Tween;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PetInventory : MonoBehaviour
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private DOTweenBase tweener;

        [SerializeField]
        private Transform descriptionViewParent;

        [SerializeField]
        private PetDescriptionView descriptionViewPrefab;

        private readonly Dictionary<int, PetDescriptionView> _views = new();

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void Initialize()
        {
            var petSheet = TableSheets.Instance.PetSheet;
            foreach (var row in petSheet)
            {
                var view = Instantiate(descriptionViewPrefab, descriptionViewParent);
                view.Initialize(row);
                _views[row.Id] = view;
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
            tweener.Play();

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
