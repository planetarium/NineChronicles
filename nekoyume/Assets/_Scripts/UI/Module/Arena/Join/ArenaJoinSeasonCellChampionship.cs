using Nekoyume.UI.Module.Arena.Emblems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinSeasonCellChampionship : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        public Animator Animator => _animator;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private TextMeshProUGUI _championshipId;

        [SerializeField]
        private SeasonArenaEmblem[] _seasonEmblems;

        public event System.Action OnClick = delegate { };

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClick.Invoke());
        }

        public void Show(ArenaJoinSeasonItemData itemData, bool selected)
        {
            _championshipId.text = itemData.RoundData.ChampionshipId.ToString();
            for (var i = 0; i < _seasonEmblems.Length; i++)
            {
                var _seasonEmblem = _seasonEmblems[i];
                if (itemData.ChampionshipSeasonNumbers.Count > i)
                {
                    _seasonEmblem.SetData(itemData.ChampionshipSeasonNumbers[i], true);
                    _seasonEmblem.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    _seasonEmblem.transform.parent.gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
