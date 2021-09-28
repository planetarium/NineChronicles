using System.Linq;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Nekoyume.UI.Module
{
    public class ShopBuyWishItemView :  MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image gradeImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI enhancementText;
        [SerializeField] private Button itemButton;


        public void SetData(ShopItem item, System.Action callback)
        {
            ItemSheet.Row row = Game.Game.instance.TableSheets.ItemSheet.Values
                .FirstOrDefault(r => r.Id == item.ItemBase.Value.Id);
            Sprite gradeSprite = SpriteHelper.GetItemBackground(row.Grade);
            gradeImage.overrideSprite = gradeSprite;
            gradeImage.enabled = true;

            var itemSprite = SpriteHelper.GetItemIcon(row.Id);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(row.Id.ToString());

            iconImage.overrideSprite = itemSprite;
            enhancementText.text = item.Level.Value > 0 ? $"+{item.Level.Value}" : string.Empty;

            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(callback.Invoke);
        }
    }
}
