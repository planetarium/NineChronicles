using Nekoyume.L10n;
using Nekoyume.TableData.Pet;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class PetDescriptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        public void SetData(PetOptionSheet.Row optionRow)
        {
            titleText.text = L10nManager.Localize($"PET_NAME_{optionRow.PetId}");

            gameObject.SetActive(true);
        }
    }
}
