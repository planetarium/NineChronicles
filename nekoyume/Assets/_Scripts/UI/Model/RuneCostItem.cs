using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Model
{
    public class RuneCostItem : MonoBehaviour
    {
        [SerializeField]
        private RuneCostType costType;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private GameObject effect;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private Button button;

        public RuneCostType CostType => costType;
        public Sprite Icon => iconImage.sprite;
        private System.Action _callback;
        private int _defaultCount;

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                if (lockObject.activeSelf)
                {
                    return;
                }

                _callback?.Invoke();
            });
        }

        public void Set(int count, bool isEnough, System.Action callback, Sprite icon = null)
        {
            _defaultCount = count;
            countText.text = count > 0 ? count.ToCurrencyNotation() : string.Empty;
            countText.color = isEnough ? Color.white : Palette.GetColor(ColorType.TextDenial);
            effect.SetActive(count > 0 && isEnough);
            _callback = callback;
            if (icon != null)
            {
                iconImage.sprite = icon;
            }

            lockObject.SetActive(count == 0);
        }

        public void UpdateCount(int tryCount)
        {
            var count = _defaultCount * tryCount;
            countText.text = _defaultCount > 0 ? count.ToCurrencyNotation() : string.Empty;
        }
    }
}
