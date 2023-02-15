using Nekoyume.Game;
using Nekoyume.UI.Tween;
using System.Collections.Generic;
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

        private readonly List<PetDescriptionView> _viewes = new();

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
                _viewes.Add(view);
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
