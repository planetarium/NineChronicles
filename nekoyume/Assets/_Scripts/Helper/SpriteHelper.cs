using Nekoyume.Data;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class SpriteHelper
    {
        private const string CharacterDefaultPath = "UI/Icons/Character/100000";
        private const string CharacterPathFormat = "UI/Icons/Character/{0}";

        private const string ItemDefaultPath = "UI/Icons/Item/100000";
        private const string ItemPathFormat = "UI/Icons/Item/{0}";
        
        private const string ItemBackgroundDefaultPath = "UI/Textures/item_bg_0";
        private const string ItemBackgroundPathFormat = "UI/Textures/item_bg_{0}";

        public static Sprite GetCharacterIcon(int characterId)
        {
            return Resources.Load<Sprite>(string.Format(CharacterPathFormat, characterId)) ??
                   Resources.Load<Sprite>(CharacterDefaultPath);
        }

        public static Sprite GetItemIcon(int itemId)
        {
            var path = ItemDefaultPath;
            if (Game.Game.instance.TableSheets.ItemSheet.ContainsKey(itemId))
            {
                path = string.Format(ItemPathFormat, itemId);
            }

            return Resources.Load<Sprite>(path);
        }

        public static Sprite GetItemBackground(int grade)
        {
            return Resources.Load<Sprite>(string.Format(ItemBackgroundPathFormat, grade)) ??
                   Resources.Load<Sprite>(ItemBackgroundDefaultPath);
        }
    }
}
