using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Emblems
{
    public class SeasonArenaEmblem : MonoBehaviour
    {
        [SerializeField] private GameObject _normal;

        [SerializeField] private GameObject _disable;

        [SerializeField] private TextMeshProUGUI[] _seasonNumbers;

        public void Show(int seasonNumber, bool isNormal)
        {
            SetData(seasonNumber, isNormal);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetData(int seasonNumber, bool isNormal)
        {
            if (_normal)
            {
                _normal.SetActive(isNormal);
            }

            if (_disable)
            {
                _disable.SetActive(!isNormal);
            }

            foreach (var text in _seasonNumbers)
            {
                text.text = seasonNumber.ToString();
            }
        }
    }
}