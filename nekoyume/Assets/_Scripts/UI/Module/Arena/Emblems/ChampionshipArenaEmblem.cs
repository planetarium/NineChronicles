using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Emblems
{
    public class ChampionshipArenaEmblem : MonoBehaviour
    {
        [SerializeField] private GameObject _normal;

        [SerializeField] private GameObject _disable;

        [SerializeField] private TextMeshProUGUI[] _championshipNumbers;

        public void Show(int championshipId, bool isNormal)
        {
            SetData(championshipId, isNormal);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetData(int championshipId, bool isNormal)
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
                text.text = championshipId.ToString();
            }
        }
    }
}