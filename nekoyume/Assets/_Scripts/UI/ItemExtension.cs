using UnityEngine;
using Nekoyume.Model.Item;
using Nekoyume.Helper;

namespace Nekoyume.UI
{
    public static class ItemExtension
    {
        public static Sprite GetIconSprite(this ItemBase item)
        {
            return SpriteHelper.GetItemIcon(item.Data.Id);
        }

        public static Sprite GetBackgroundSprite(this ItemBase item)
        {
            return SpriteHelper.GetItemBackground(item.Data.Grade);
        }
    }
}
