using System;
using Nekoyume.Data;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
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

        private const string ItemBackgroundDefaultPath = "UI/Textures/item_bg_1";
        private const string ItemBackgroundPathFormat = "UI/Textures/item_bg_{0}";

        private const string SmallItemBackgroundDefaultPath = "UI/Textures/item_bg_1_s";
        private const string SmallItemBackgroundPathFormat = "UI/Textures/item_bg_{0}_s";

        private const string BuffIconDefaultPath = "UI/Icons/Buff/icon_buff_resurrection";
        private const string BuffIconPathFormat = "UI/Icons/Buff/{0}";

        private const string PlayerSpineTextureWeaponPathFormat = "Character/PlayerSpineTexture/Weapon/{0}";
        private const string AreaAttackCutscenePath = "Character/PlayerSpineTexture/AreaAttackCutscene/";

        private const string RankIconPath = "UI/Textures/UI_icon_ranking_{0}";

        private const string TitleFramePathFormat = "UI/Textures/TitleFrames/{0}";
        private static readonly string TitleFrameDefaultPath = string.Format(TitleFramePathFormat, 49900001);

        private const string MenuIllustratePathFormat = "UI/Textures/MenuIllustrates/{0}";

        private static readonly string MenuIllustrateDefaultPath =
            string.Format(MenuIllustratePathFormat, "UI_bg_combination");

        private const string MailIconPathFormat = "UI/Icons/Mail/{0}";

        private static readonly string MailIconDefaultPath =
            string.Format(MailIconPathFormat, "icon_mail_system");

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

        public static Sprite GetTitleFrame(int titleId)
        {
            return Resources.Load<Sprite>(string.Format(TitleFramePathFormat, titleId)) ??
                   Resources.Load<Sprite>(TitleFrameDefaultPath);
        }

        public static Sprite GetMenuIllustration(string menuName)
        {
            Sprite result = null;
            switch (menuName)
            {
                case nameof(ArenaJoin):
                    result = Resources.Load<Sprite>(
                        string.Format(MenuIllustratePathFormat, "UI_bg_ranking"));
                    break;
                case nameof(Shop):
                    result = Resources.Load<Sprite>(
                        string.Format(MenuIllustratePathFormat, "UI_bg_shop"));
                    break;
                case "Mimisbrunnr":
                    result = Resources.Load<Sprite>(
                        string.Format(MenuIllustratePathFormat, "UI_bg_mimisbrunnr"));
                    break;
            }

            return result ? result : Resources.Load<Sprite>(MenuIllustrateDefaultPath);
        }

        public static Sprite GetMailIcon(MailType mailType)
        {
            Sprite result = null;
            switch (mailType)
            {
                case MailType.Workshop:
                    result = Resources.Load<Sprite>(
                        string.Format(MailIconPathFormat, "icon_mail_workshop"));
                    break;
                case MailType.Auction:
                    result = Resources.Load<Sprite>(
                        string.Format(MailIconPathFormat, "icon_mail_auction"));
                    break;
                case MailType.System:
                    result = Resources.Load<Sprite>(
                        string.Format(MailIconPathFormat, "icon_mail_system"));
                    break;
                case MailType.Grinding:
                    result = Resources.Load<Sprite>(
                        string.Format(MailIconPathFormat, "icon_mail_grind"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mailType), mailType, null);
            }

            return result ? result : Resources.Load<Sprite>(MailIconDefaultPath);
        }
    }
}
