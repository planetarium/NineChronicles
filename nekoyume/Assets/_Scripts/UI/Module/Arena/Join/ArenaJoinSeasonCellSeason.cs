using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinSeasonCellSeason : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        public Animator Animator => _animator;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private TextMeshProUGUI _seasonNumber;

        public event System.Action OnClick = delegate { };

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClick.Invoke());
        }

        public void Show(ArenaJoinSeasonItemData itemData, bool selected)
        {
            _seasonNumber.text = itemData.SeasonNumber.ToString();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
