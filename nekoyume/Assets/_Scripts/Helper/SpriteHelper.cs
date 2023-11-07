using System;
using Nekoyume.AssetBundleHelper;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class SpriteHelper
    {
        private const string CharacterIconBundle = "UI/Icons/Character";
        private const string CharacterIconDefaultPath = "100000";
        private const string CharacterIconPathFormat = "{0}";

        private const string ItemIconBundle = "UI/Icons/Item";
        private const string ItemIconDefaultPath = "100000";
        private const string ItemIconPathFormat = "{0}";

        private const string DccIconBundle = "PFP";
        private const string DccIconPathFormat = "{0}";

        private const string ProfileFrameBundle = "UI/Icons/Item";
        private const string ProfileFrameDefaultPath = "character_frame";

        private const string ItemBackgroundBundle = "UI/Textures";
        private const string ItemBackgroundDefaultPath = "item_bg_1";
        private const string ItemBackgroundPathFormat = "item_bg_{0}";

        private const string BuffIconBundle = "UI/Icons/Buff";
        private const string BuffIconDefaultPath = "icon_buff_resurrection";
        private const string BuffIconPathFormat = "{0}";

        private const string PlayerSpineBundle = "Character/Player";
        private const string PlayerSpineTextureWeaponPathFormat = "{0}";
        private const string AreaAttackCutsceneFormat = "{0}";

        private const string RankIconBundle = "UI/Textures";
        private const string RankIconPath = "UI_icon_ranking_{0}";

        private const string MailIconBundle = "UI/Icons/Mail";
        private const string MailIconPathFormat = "{0}";
        private static readonly string MailIconDefaultPath =
            string.Format(MailIconPathFormat, "icon_mail_system");

        private const string WorldmapBackgroundBundle = "UI/Textures";
        private const string WorldmapBackgroundPathFormat = "battle_UI_BG_{0}_{1:D2}";
        private const string WorldmapBackgroundDefaultPathFormat = "battle_UI_BG_01_{0:D2}";

        private const string DialogNPCPortaitBundle = "Images";
        private const string DialogNPCPortaitPathFormat = "NPC_{0}";
        private const string DialogCharacterPortaitPathFormat = "character_{0}";

        private const string FavIconBundle = "UI/Icons/FungibleAssetValue";
        private const string FavIconPathFormat = "{0}";
        private const string DefaultFavIconPathFormat = "RUNE_ADVENTURER";

        public static Sprite GetCharacterIcon(int characterId)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(CharacterIconBundle, string.Format(CharacterIconPathFormat, characterId)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(CharacterIconBundle, CharacterIconDefaultPath);
        }

        public static Sprite GetItemIcon(int itemId)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(ItemIconBundle, string.Format(ItemIconPathFormat, itemId)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(ItemIconBundle, ItemIconDefaultPath);
        }

        public static Sprite GetDccProfileIcon(int dccId)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(DccIconBundle, string.Format(DccIconPathFormat, dccId)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(CharacterIconBundle, CharacterIconDefaultPath);
        }

        public static Sprite GetProfileFrameIcon(string frameName)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(ItemIconBundle, string.Format(ItemIconPathFormat, frameName)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(ProfileFrameBundle, ProfileFrameDefaultPath);
        }

        public static Sprite GetItemBackground(int grade)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(ItemBackgroundBundle, string.Format(ItemBackgroundPathFormat, grade)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(ItemBackgroundBundle, ItemBackgroundDefaultPath);
        }

        public static Sprite GetBuffIcon(string iconResource)
        {
            if (string.IsNullOrEmpty(iconResource))
            {
                return AssetBundleLoader.LoadAssetBundle<Sprite>(BuffIconBundle, BuffIconDefaultPath);
            }

            return AssetBundleLoader.LoadAssetBundle<Sprite>(BuffIconBundle, string.Format(BuffIconPathFormat, iconResource)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(BuffIconBundle, BuffIconDefaultPath);
        }

        public static Sprite GetPlayerSpineTextureWeapon(int equipmentId)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(PlayerSpineBundle, string.Format(PlayerSpineTextureWeaponPathFormat, equipmentId)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(PlayerSpineBundle, string.Format(PlayerSpineTextureWeaponPathFormat, GameConfig.DefaultAvatarWeaponId));
        }

        public static Sprite GetAreaAttackCutsceneSprite(int id)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(PlayerSpineBundle, string.Format(AreaAttackCutsceneFormat, id)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(PlayerSpineBundle, string.Format(AreaAttackCutsceneFormat, GameConfig.DefaultAvatarArmorId));
        }

        public static Sprite GetRankIcon(int rank)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(RankIconBundle, string.Format(RankIconPath, rank.ToString("D2")));
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

            var result = AssetBundleLoader.LoadAssetBundle<Sprite>(MailIconBundle, string.Format(MailIconPathFormat, fileName));
            return result ? result : AssetBundleLoader.LoadAssetBundle<Sprite>(MailIconBundle, MailIconDefaultPath);
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
                RaidRewardMail => AssetBundleLoader.LoadAssetBundle<Sprite>(
                    MailIconBundle, string.Format(MailIconPathFormat, "icon_mail_worldBoss")),
                _ => null
            };
        }

        public static Sprite GetWorldMapBackground(string imageKey, int pageIndex)
        {
            var path = string.Format(WorldmapBackgroundPathFormat, imageKey, pageIndex);
            var sprite = AssetBundleLoader.LoadAssetBundle<Sprite>(WorldmapBackgroundBundle, path);
            if (sprite)
            {
                return sprite;
            }

            var defaultPath = string.Format(WorldmapBackgroundDefaultPathFormat, pageIndex);
            var defaultSprite = AssetBundleLoader.LoadAssetBundle<Sprite>(WorldmapBackgroundBundle, defaultPath);
            return defaultSprite;
        }

        public static Sprite GetDialogPortrait(string key, bool isNPC = true)
        {
            var path = string.Format(isNPC ?
                DialogNPCPortaitPathFormat : DialogCharacterPortaitPathFormat, key);
            return AssetBundleLoader.LoadAssetBundle<Sprite>(DialogNPCPortaitBundle, path);
        }

        public static Sprite GetFavIcon(string ticker)
        {
            return AssetBundleLoader.LoadAssetBundle<Sprite>(FavIconBundle, string.Format(FavIconPathFormat, ticker)) ??
                   AssetBundleLoader.LoadAssetBundle<Sprite>(FavIconBundle, DefaultFavIconPathFormat);
        }
    }
}
