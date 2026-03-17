using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class StakingInterestBenefitsView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;

        public void Set(ItemBase itemBase, int count)
        {
            iconImage.sprite = BaseItemView.GetItemIcon(itemBase);
            countText.text = $"+{count}";
        }

        public void Set(int runeId, int count)
        {
            iconImage.sprite = TableSheets.Instance.ItemSheet.TryGetValue(runeId, out var row)
                ? SpriteHelper.GetItemIcon(runeId, row.ItemSubType, row.Grade)
                : SpriteHelper.GetItemIcon(runeId);
            countText.text = $"+{count}";
        }

        public void Set(string ticker, int count, bool useCurrencyNotation = false)
        {
            iconImage.sprite = SpriteHelper.GetFavIcon(ticker);
            countText.text = $"+{(useCurrencyNotation ? count.ToCurrencyNotation() : count)}";
        }
    }
}
