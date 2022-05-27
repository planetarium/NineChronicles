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
        private TextMeshProUGUI[] _seasonIds;

        public event System.Action OnClick = delegate { };

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClick.Invoke());
        }

        public void Show(ArenaJoinSeasonItemData itemData, bool selected)
        {
            _championshipId.text = itemData.ChampionshipId.ToString();
            for (var i = 0; i < _seasonIds.Length; i++)
            {
                var seasonId = _seasonIds[i];
                if (itemData.ChampionshipSeasonIds.Length > i)
                {
                    seasonId.text = itemData.ChampionshipSeasonIds[i].ToString();
                    seasonId.gameObject.SetActive(true);
                }
                else
                {
                    seasonId.gameObject.SetActive(false);
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
