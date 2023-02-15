using Nekoyume.Game;
using Nekoyume.UI.Tween;
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

        private int _currentIndex;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void Initialize()
        {
            var optionSheet = TableSheets.Instance.PetOptionSheet;
            foreach (var row in optionSheet)
            {
                var view = Instantiate(descriptionViewPrefab, descriptionViewParent);
                view.SetData(row);
            }
        }

        public void Toggle(int slotIndex)
        {
            if (gameObject.activeSelf &&
                _currentIndex == slotIndex)
            {
                Hide();
                return;
            }

            _currentIndex = slotIndex;
            UpdateView(_currentIndex);

            if (!gameObject.activeSelf)
            {
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
