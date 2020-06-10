using Nekoyume.Data;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class SpriteHelper
    {
        private const string CharacterIconDefaultPath = "UI/Icons/Character/100000";
        private const string CharacterIconPathFormat = "UI/Icons/Character/{0}";

        private const string SkillIconDefaultPath = "UI/Icons/Skill/100000";

        private const string ItemIconDefaultPath = "UI/Icons/Item/100000";
        private const string ItemIconPathFormat = "UI/Icons/Item/{0}";

        private const string ItemBackgroundDefaultPath = "UI/Textures/item_bg_1";
        private const string ItemBackgroundPathFormat = "UI/Textures/item_bg_{0}";

        private const string SmallItemBackgroundDefaultPath = "UI/Textures/item_bg_1_s";
        private const string SmallItemBackgroundPathFormat = "UI/Textures/item_bg_{0}_s";

        private const string BuffIconDefaultPath = "UI/Icons/Buff/icon_buff_resurrection";
        private const string BuffIconPathFormat = "UI/Icons/Buff/{0}";

        private const string PlayerSpineTextureEyeCostumeOpenDefaultPath = "Character/PlayerSpineTexture/EyeCostume/eye_red_open";
        private const string PlayerSpineTextureEyeCostumeHalfDefaultPath = "Character/PlayerSpineTexture/EyeCostume/eye_red_half";
        private const string PlayerSpineTextureEyeCostumePathFormat = "Character/PlayerSpineTexture/EyeCostume/{0}";

        private const string PlayerSpineTextureTailCostumeDefaultPath = "Character/PlayerSpineTexture/TailCostume/tail_0001";
        private const string PlayerSpineTextureTailCostumePathFormat = "Character/PlayerSpineTexture/TailCostume/{0}";

        private const string PlayerSpineTextureWeaponPathFormat = "Character/PlayerSpineTexture/Weapon/{0}";

        private const string RankIconPath = "UI/Textures/UI_icon_ranking_{0}";

        public static Sprite GetCharacterIcon(int characterId)
        {
            return Resources.Load<Sprite>(string.Format(CharacterIconPathFormat, characterId)) ??
                   Resources.Load<Sprite>(CharacterIconDefaultPath);
        }

        public static Sprite GetItemIcon(int itemId)
        {
            var path = ItemIconDefaultPath;
            if (Game.Game.instance.TableSheets.ItemSheet.ContainsKey(itemId))
            {
                path = string.Format(ItemIconPathFormat, itemId);
            }

            return Resources.Load<Sprite>(path);
        }

        public static Sprite GetItemBackground(int grade)
        {
            return Resources.Load<Sprite>(string.Format(ItemBackgroundPathFormat, grade)) ??
                   Resources.Load<Sprite>(ItemBackgroundDefaultPath);
        }

        public static Sprite GetSmallItemBackground(int grade)
        {
            return Resources.Load<Sprite>(string.Format(SmallItemBackgroundPathFormat, grade)) ??
                   Resources.Load<Sprite>(SmallItemBackgroundDefaultPath);
        }

        public static Sprite GetSkillIcon(int skillId)
        {
            var path = $"UI/Icons/Skill/{skillId}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite)
            {
                return sprite;
            }

            sprite = Resources.Load<Sprite>(SkillIconDefaultPath);

            return sprite;
        }

        public static Sprite GetBuffIcon(string iconResource)
        {
            if (string.IsNullOrEmpty(iconResource))
            {
                return Resources.Load<Sprite>(BuffIconDefaultPath);
            }

            return Resources.Load<Sprite>(string.Format(BuffIconPathFormat, iconResource)) ??
                   Resources.Load<Sprite>(BuffIconDefaultPath);
        }

        public static Sprite GetPlayerSpineTextureEyeCostumeOpen(string eyeCostumeOpenResource)
        {
            if (string.IsNullOrEmpty(eyeCostumeOpenResource))
            {
                return Resources.Load<Sprite>(PlayerSpineTextureEyeCostumeOpenDefaultPath);
            }

            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyeCostumePathFormat, eyeCostumeOpenResource)) ??
                   Resources.Load<Sprite>(PlayerSpineTextureEyeCostumeOpenDefaultPath);
        }

        public static Sprite GetPlayerSpineTextureEyeCostumeHalf(string eyeCostumeHalfResource)
        {
            if (string.IsNullOrEmpty(eyeCostumeHalfResource))
            {
                return Resources.Load<Sprite>(PlayerSpineTextureEyeCostumeHalfDefaultPath);
            }

            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyeCostumePathFormat, eyeCostumeHalfResource)) ??
                   Resources.Load<Sprite>(PlayerSpineTextureEyeCostumeHalfDefaultPath);
        }

        public static Sprite GetPlayerSpineTextureTailCostume(string tailCostumeResource)
        {
            if (string.IsNullOrEmpty(tailCostumeResource))
            {
                return Resources.Load<Sprite>(PlayerSpineTextureTailCostumeDefaultPath);
            }

            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureTailCostumePathFormat, tailCostumeResource)) ??
                   Resources.Load<Sprite>(PlayerSpineTextureTailCostumeDefaultPath);
        }

        public static Sprite GetPlayerSpineTextureWeapon(int equipmentId)
        {
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPathFormat, equipmentId)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPathFormat, GameConfig.DefaultAvatarWeaponId));
        }

        public static Sprite GetRankIcon(int rank)
        {
            return Resources.Load<Sprite>(string.Format(RankIconPath, rank.ToString("D2")));
        }
    }
}
