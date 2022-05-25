using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    public class ArenaJoinSeasonCellOffseason : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        public Animator Animator => _animator;

        [SerializeField]
        private Button _button;

        public event System.Action OnClick = delegate { };

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClick.Invoke());
        }

        public void Show(ArenaJoinSeasonItemData itemData, bool selected)
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
