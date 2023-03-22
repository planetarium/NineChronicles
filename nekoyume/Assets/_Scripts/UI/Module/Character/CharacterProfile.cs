using Libplanet;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CharacterProfile : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI characterInfoText;
        [SerializeField] private DetailedCharacterView characterView;

        public void Set(int level, string nameWithHash, int fullCostumeOrArmorId, Address address)
        {
            characterInfoText.text = nameWithHash;

            if (Dcc.instance.Avatars.TryGetValue(address.ToString(), out var dccId))
            {
                characterView.SetByDccId(dccId, level);
            }
            else
            {
                characterView.SetByFullCostumeOrArmorId(fullCostumeOrArmorId, level);
            }
        }
    }
}
