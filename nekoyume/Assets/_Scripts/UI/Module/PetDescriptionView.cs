using Coffee.UIEffects;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.TableData;
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
        private GameObject inUseObject;

        [SerializeField]
        private GameObject descriptionObject;

        [SerializeField]
        private GameObject emptyObject;

#region Initialized First

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private Image gradeBg;

        [SerializeField]
        private UIHsvModifier gradeBgHsv;

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

        public int Grade { get; private set; }

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
            Grade = petRow.Grade;
            titleText.text = L10nManager.Localize($"PET_NAME_{petRow.Id}");

            var itemViewData = itemViewDataObject.GetItemViewData(petRow.Grade);
            gradeBg.overrideSprite = itemViewData.GradeBackground;
            gradeBgHsv.range = itemViewData.GradeHsvRange;
            gradeBgHsv.hue = itemViewData.GradeHsvHue;
            gradeBgHsv.value = itemViewData.GradeHsvValue;
            gradeBgHsv.saturation = itemViewData.GradeHsvSaturation;

            var petData = petDataObject.GetPetData(petRow.Id);
            petIconImage.overrideSprite = petData.icon;

            equippedObject.SetActive(false);
            gameObject.SetActive(true);
            descriptionObject.SetActive(false);
            emptyObject.SetActive(true);
        }

        public void InitializeEmpty(System.Action<int?> onClick)
        {
            _onClick = onClick;
            _petId = null;
            descriptionObject.SetActive(false);
            emptyObject.SetActive(true);
            gameObject.SetActive(true);
            dimmedImage.enabled = false;
        }

        public void SetData(PetInventory.PetDescriptionData data)
        {
            if (data.PetId == default)
            {
                return;
            }

            UpdateView(data);
            descriptionText.text = data.Description;
        }

        private void UpdateView(PetInventory.PetDescriptionData data)
        {
            levelText.text = $"Lv.{data.Level}";
            dimmedImage.enabled = !data.HasState || !data.IsAppliable;
            equippedObject.SetActive(data.Equipped);
            if (button)
            {
                button.gameObject.SetActive(data.IsAppliable && !data.Equipped);
                inUseObject.SetActive(data.Equipped);
            }

            descriptionObject.SetActive(true);
            emptyObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
