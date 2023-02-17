using Mono.Cecil;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
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

        [SerializeField]
        private Image dimmedImage;
        #endregion

        private int? _petId;

        private System.Action<int?> _onClick;

        public bool IsAvailable { get; private set; }

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
            IsAvailable = false;
        }

        public void InitializeEmpty(System.Action<int?> onClick)
        {
            _onClick = onClick;
            _petId = null;
            equipObject.SetActive(false);
            emptyObject.SetActive(true);
            gameObject.SetActive(true);
            dimmedImage.enabled = false;
            IsAvailable = false;
        }

        public void SetData(int petId)
        {
            IsAvailable = false;
            var tableSheets = TableSheets.Instance;
            var petLevel = 1;

            PetState petState;
            if (!tableSheets.PetOptionSheet.TryGetValue(petId, out var optionRow))
            {
                gameObject.SetActive(false);
                return;
            }
            else if (States.Instance.PetStates.TryGetPetState(petId, out petState))
            {
                petLevel = petState.Level;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(petLevel, out var optionInfo))
            {
                gameObject.SetActive(false);
                return;
            }

            descriptionText.text = L10nManager.Localize(
                $"PET_DESCRIPTION_{optionInfo.OptionType}",
                optionInfo.OptionValue);

            var hasPetState = petState != null;
            var equipped = hasPetState &&
                (States.Instance.PetStates.IsLocked(petId) ||
                petState.UnlockedBlockIndex > Game.Game.instance.Agent.BlockIndex);

            dimmedImage.enabled = !hasPetState;
            equippedObject.SetActive(equipped);
            if (button)
            {
                button.gameObject.SetActive(!equipped);
            }
            equipObject.SetActive(true);
            emptyObject.SetActive(false);
            gameObject.SetActive(true);
            IsAvailable = !hasPetState;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
