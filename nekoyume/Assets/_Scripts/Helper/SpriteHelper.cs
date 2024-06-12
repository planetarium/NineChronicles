using System;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class SpriteHelper
    {
        private const string CharacterIconDefaultPath = "UI/Icons/Character/100000";
        private const string CharacterIconPathFormat = "UI/Icons/Character/{0}";

        private const string ItemIconDefaultPath = "UI/Icons/Item/100000";
        private const string ItemIconPathFormat = "UI/Icons/Item/{0}";

        private const string DccIconPathFormat = "PFP/{0}";
        private const string ProfileFrameDefaultPath = "UI/Icons/Item/{character_frame}";

        private const string ItemBackgroundDefaultPath = "UI/Textures/item_bg_1";
        private const string ItemBackgroundPathFormat = "UI/Textures/item_bg_{0}";

        private const string BuffIconDefaultPath = "UI/Icons/Buff/icon_buff_resurrection";
        private const string BuffIconPathFormat = "UI/Icons/Buff/{0}";

        private const string PlayerSpineTextureWeaponPathFormat = "Character/PlayerSpineTexture/Weapon/{0}";
        private const string AreaAttackCutscenePath = "Character/PlayerSpineTexture/AreaAttackCutscene/";

        private const string RankIconPath = "UI/Textures/UI_icon_ranking_{0}";

        private const string MailIconPathFormat = "UI/Icons/Mail/{0}";
        private static readonly string MailIconDefaultPath =
            string.Format(MailIconPathFormat, "icon_mail_system");

        private const string WorldmapBackgroundPathFormat = "UI/Textures/00_WorldMap/battle_UI_BG_{0}_{1:D2}";
        private const string WorldmapBackgroundDefaultPathFormat = "UI/Textures/00_WorldMap/battle_UI_BG_01_{0:D2}";

        private const string DialogNPCPortaitPathFormat = "Images/npc/NPC_{0}";
        private const string DialogCharacterPortaitPathFormat = "Images/character_{0}";

        private const string FavIconPathFormat = "UI/Icons/FungibleAssetValue/{0}";
        private const string DefaultFavIconPathFormat = "UI/Icons/FungibleAssetValue/RUNE_ADVENTURER";

        private const string BigCharacterIconDefaultPath = "UI/Icons/BigCharacter/Default";
        private const string BigCharacterIconPathFormat = "UI/Icons/BigCharacter/{0}";
        private const string BigCharacterIconFacePathFormat = "UI/Icons/BigCharacter/Face/{0}";
        private const string BigCharacterIconBodyPathFormat = "UI/Icons/BigCharacter/Body/{0}";

        public static Sprite GetBigCharacterIcon(int characterId)
        {
            return Resources.Load<Sprite>(string.Format(BigCharacterIconPathFormat, characterId)) ??
                   Resources.Load<Sprite>(BigCharacterIconDefaultPath);
        }

        public static GameObject GetBigCharacterIconFace(int characterId)
        {
            return Resources.Load<GameObject>(string.Format(BigCharacterIconFacePathFormat, characterId));
        }

        public static GameObject GetBigCharacterIconBody(int characterId)
        {
            return Resources.Load<GameObject>(string.Format(BigCharacterIconBodyPathFormat, characterId));
        }

        public static Sprite GetCharacterIcon(int characterId)
        {
            return Resources.Load<Sprite>(string.Format(CharacterIconPathFormat, characterId)) ??
                   Resources.Load<Sprite>(CharacterIconDefaultPath);
        }

        public static Sprite GetItemIcon(int itemId)
        {
            return Resources.Load<Sprite>(string.Format(ItemIconPathFormat, itemId)) ??
                   Resources.Load<Sprite>(ItemIconDefaultPath);
        }

        public static Sprite GetDccProfileIcon(int dccId)
        {
            return Resources.Load<Sprite>(string.Format(DccIconPathFormat, dccId)) ??
                   Resources.Load<Sprite>(CharacterIconDefaultPath);
        }

        public static Sprite GetProfileFrameIcon(string frameName)
        {
            return Resources.Load<Sprite>(string.Format(ItemIconPathFormat, frameName)) ??
                   Resources.Load<Sprite>(ProfileFrameDefaultPath);
        }

        public static Sprite GetItemBackground(int grade)
        {
            return Resources.Load<Sprite>(string.Format(ItemBackgroundPathFormat, grade)) ??
                   Resources.Load<Sprite>(ItemBackgroundDefaultPath);
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

        public static Sprite GetPlayerSpineTextureWeapon(int equipmentId)
        {
            return Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPathFormat, equipmentId)) ??
                   Resources.Load<Sprite>(string.Format(PlayerSpineTextureWeaponPathFormat, GameConfig.DefaultAvatarWeaponId));
        }

        public static Sprite GetAreaAttackCutsceneSprite(int id)
        {
            return Resources.Load<Sprite>($"{AreaAttackCutscenePath}{id}") ??
                   Resources.Load<Sprite>($"{AreaAttackCutscenePath}{GameConfig.DefaultAvatarArmorId}");
        }

        public static Sprite GetRankIcon(int rank)
        {
            return Resources.Load<Sprite>(string.Format(RankIconPath, rank.ToString("D2")));
        }

        public static Sprite GetMailIcon(MailType mailType)
        {
            var fileName = string.Empty;
            switch (mailType)
            {
                case MailType.Workshop:
                    fileName = "icon_mail_workshop";
                    break;
                case MailType.Auction:
                    fileName = "icon_mail_auction";
                    break;
                case MailType.System:
                    fileName = "icon_mail_system";
                    break;
                case MailType.Grinding:
                    fileName = "icon_mail_grind";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mailType), mailType, null);
            }

            var result = Resources.Load<Sprite>(string.Format(MailIconPathFormat, fileName));
            return result ? result : Resources.Load<Sprite>(MailIconDefaultPath);
        }

        /// <summary>
        /// GetMailIcon method but not considering MailType.
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public static Sprite GetLocalMailIcon(Mail mail)
        {
            return mail switch
            {
                RaidRewardMail => Resources.Load<Sprite>(
                    string.Format(MailIconPathFormat, "icon_mail_worldBoss")),
                _ => null
            };
        }

        public static Sprite GetWorldMapBackground(string imageKey, int pageIndex)
        {
            var path = string.Format(WorldmapBackgroundPathFormat, imageKey, pageIndex);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite)
            {
                return sprite;
            }

            var defaultPath = string.Format(WorldmapBackgroundDefaultPathFormat, pageIndex);
            var defaultSprite = Resources.Load<Sprite>(defaultPath);
            return defaultSprite;
        }

        public static Sprite GetDialogPortrait(string key, bool isNPC = true)
        {
            var path = string.Format(isNPC ?
                DialogNPCPortaitPathFormat : DialogCharacterPortaitPathFormat, key);
            return Resources.Load<Sprite>(path);
        }

        public static Sprite GetFavIcon(string ticker)
        {
            return Resources.Load<Sprite>(string.Format(FavIconPathFormat, ticker)) ??
                   Resources.Load<Sprite>(DefaultFavIconPathFormat);
        }
    }
}
