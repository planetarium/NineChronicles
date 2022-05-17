using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena
{
    public class ArenaJoinSeasonCellOffseason : MonoBehaviour
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
            UpdateSelected(selected);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateSelected(bool selected)
        {
            _image.color = selected
                ? new Color32(92, 25, 144, 218)
                : new Color32(41, 18, 58, 218);
        }
    }
}
