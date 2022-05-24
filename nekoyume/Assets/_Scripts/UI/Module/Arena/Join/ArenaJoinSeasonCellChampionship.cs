using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena
{
    public class ArenaJoinSeasonCellChampionship : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField]
        private TextMeshProUGUI _name;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Button _button;

        public event System.Action OnClick = delegate { };

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClick.Invoke());
        }

        public void Show(ArenaJoinSeasonItemData itemData, bool selected)
        {
            _name.text = itemData.name;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
