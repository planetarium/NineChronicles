using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
using System.Security.Policy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PetDescriptionView : MonoBehaviour
    {
        [SerializeField]
        private ItemViewDataScriptableObject itemViewDataObject;

        [SerializeField]
        private PetScriptableObject petDataObject;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject equipObject;

        [SerializeField]
        private GameObject emptyObject;

        #region Initialized First
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private Image gradeBg;

        [SerializeField]
        private Image petIconImage;
        #endregion

        #region Updated on enabled
        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private GameObject equippedObject;

        [SerializeField]
        private TextMeshProUGUI descriptionText;
        #endregion

        private int? _petId;

        private System.Action<int?> _onClick;

        private void Awake()
        {
            if (button)
            {
                button.onClick.AddListener(() => _onClick?.Invoke(_petId));
            }
        }

        public void Initialize(PetSheet.Row petRow, System.Action<int?> onClick)
        {
            _onClick = onClick;
            _petId = petRow.Id;
            titleText.text = L10nManager.Localize($"PET_NAME_{petRow.Id}");
            gradeBg.overrideSprite = itemViewDataObject
                .GetItemViewData(petRow.Grade)
                .GradeBackground;

            var petData = petDataObject.GetPetData(petRow.Id);
            petIconImage.overrideSprite = petData.icon;

            equippedObject.SetActive(false);
            gameObject.SetActive(true);
            equipObject.SetActive(false);
            emptyObject.SetActive(true);
        }

        public void InitializeEmpty(System.Action<int?> onClick)
        {
            _onClick = onClick;
            _petId = null;
            equipObject.SetActive(false);
            emptyObject.SetActive(true);
            gameObject.SetActive(true);
        }

        public void SetData(PetState petState, bool equipped)
        {
            var tableSheets = TableSheets.Instance;
            if (petState is null ||
                !tableSheets.PetOptionSheet.TryGetValue(petState.PetId, out var optionRow) ||
                !optionRow.LevelOptionMap.TryGetValue(petState.Level, out var optionInfo))
            {
                equippedObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            descriptionText.text = L10nManager.Localize(
                $"PET_DESCRIPTION_{optionInfo.OptionType}",
                optionInfo.OptionValue);
            equippedObject.SetActive(equipped);
            equipObject.SetActive(true);
            emptyObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
