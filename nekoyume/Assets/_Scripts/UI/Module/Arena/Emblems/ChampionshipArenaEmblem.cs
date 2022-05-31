using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Emblems
{
    public class ChampionshipArenaEmblem : MonoBehaviour
    {
        [SerializeField]
        private GameObject _normal;

        [SerializeField]
        private GameObject _disable;

        [SerializeField]
        private TextMeshProUGUI[] _championshipNumbers;

        public void SetData(int championshipNumber, bool isNormal)
        {
            if (_normal)
            {
                _normal.SetActive(isNormal);
            }

            if (_disable)
            {
                _disable.SetActive(!isNormal);
            }

            foreach (var text in _championshipNumbers)
            {
                text.text = championshipNumber.ToString();
            }
        }
    }
}
