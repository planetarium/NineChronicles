using Nekoyume.Game;
using Nekoyume.State;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PetSelectButton : MonoBehaviour
    {
        [SerializeField]
        private PetScriptableObject petDataObject;

        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private GameObject equippedObject;

        [SerializeField]
        private GameObject selectedObject;

        [SerializeField]
        private GameObject notificationObject;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private List<GameObject> slotImages;

        public void SetData(int? petId)
        {
            selectedObject.SetActive(false);
            notificationObject.SetActive(false);

            if (!petId.HasValue ||
                !States.Instance.PetStates.TryGetPetState(petId.Value, out var petState))
            {
                emptyObject.SetActive(true);
                equippedObject.SetActive(false);
                return;
            }

            emptyObject.SetActive(false);
            equippedObject.SetActive(true);

            var petData = petDataObject.GetPetData(petState.PetId);
            iconImage.overrideSprite = petData.icon;
            levelText.text = $"Lv.{petState.Level}";

            var petGrade = TableSheets.Instance.PetSheet[petState.PetId].Grade;
            for (var i = 0; i < slotImages.Count; i++)
            {
                slotImages[i].SetActive(i == petGrade);
            }
        }
    }
}
