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

        private const string BuffIconDefaultPath = "UI/Icons/Buff/icon_buff_resurrection";
        private const string BuffIconPathFormat = "UI/Icons/Buff/{0}";

        private const string PlayerSpineTextureEarPath = "Character/PlayerSpineTexture/Ear/{0}";
        private const string PlayerSpineTextureEyePath = "Character/PlayerSpineTexture/Eye/{0}";
        private const string PlayerSpineTextureTailPath = "Character/PlayerSpineTexture/Tail/{0}";
        private const string PlayerSpineTextureWeaponPath = "Character/PlayerSpineTexture/Weapon/{0}";

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
        
        public static Sprite GetPlayerSpineTextureEarLeft(string earLeftResource)
        {
            if (string.IsNullOrEmpty(earLeftResource))
            {
                return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, GameConfig.DefaultPlayerEarLeftResource));
            }
            
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, earLeftResource)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, GameConfig.DefaultPlayerEarLeftResource));
        }
        
        public static Sprite GetPlayerSpineTextureEarRight(string earRightResource)
        {
            if (string.IsNullOrEmpty(earRightResource))
            {
                return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, GameConfig.DefaultPlayerEarRightResource));
            }
            
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, earRightResource)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureEarPath, GameConfig.DefaultPlayerEarRightResource));
        }
        
        public static Sprite GetPlayerSpineTextureEyeOpen(string eyeOpenResource)
        {
            if (string.IsNullOrEmpty(eyeOpenResource))
            {
                return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, GameConfig.DefaultPlayerEyeOpenResource));
            }
            
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, eyeOpenResource)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, GameConfig.DefaultPlayerEyeOpenResource));
        }
        
        public static Sprite GetPlayerSpineTextureEyeHalf(string eyeHalfResource)
        {
            if (string.IsNullOrEmpty(eyeHalfResource))
            {
                return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, GameConfig.DefaultPlayerEyeHalfResource));
            }
            
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, eyeHalfResource)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureEyePath, GameConfig.DefaultPlayerEyeHalfResource));
        }
        
        public static Sprite GetPlayerSpineTextureTail(string tailResource)
        {
            if (string.IsNullOrEmpty(tailResource))
            {
                return Resources.Load<Sprite>(string.Format(PlayerSpineTextureTailPath, GameConfig.DefaultPlayerTailResource));
            }
            
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureTailPath, tailResource)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureTailPath, GameConfig.DefaultPlayerTailResource));
        }
        
        public static Sprite GetPlayerSpineTextureWeapon(int equipmentId)
        {
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPath, equipmentId)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPath, GameConfig.DefaultAvatarWeaponId));
        }

        public static Sprite GetRankIcon(int rank)
        {
            return Resources.Load<Sprite>(string.Format(RankIconPath, rank.ToString("D2")));
        }
    }
}
