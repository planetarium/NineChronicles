using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
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

        private RuneCostSheet.Row _costRow;
        private int _startLevel;
        private bool _isEnough;
        private System.Action _callback;

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

        public void Set(RuneCostSheet.Row costRow, int startLevel,
            bool isEnough, System.Action callback, Sprite icon = null)
        {
            _costRow = costRow;
            _startLevel = startLevel;
            _isEnough = isEnough;
            _callback = callback;
            if (icon != null)
            {
                iconImage.sprite = icon;
            }
        }

        public void UpdateCount(int tryCount)
        {
            if (_costRow is null)
            {
                return;
            }

            var cost = _costRow.GetCostQuantity(_startLevel, tryCount, costType);
            var costExist = cost > 0;

            countText.text = cost > 0 ? cost.ToCurrencyNotation() : string.Empty;
            countText.color = _isEnough ? Color.white : Palette.GetColor(ColorType.TextDenial);
            effect.SetActive(costExist && _isEnough);
            lockObject.SetActive(!costExist);
        }
    }
}
