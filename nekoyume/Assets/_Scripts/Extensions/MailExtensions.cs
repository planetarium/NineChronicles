using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;

namespace Nekoyume
{
    public static class MailExtensions
    {


        public static string GetCellContent(this UnloadFromMyGaragesRecipientMail mail)
        {
            var format = L10nManager.Localize(
                "MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_CELL_CONTENT_FORMAT");
            return string.Format(
                format,
                mail.FungibleAssetValues?.Count() ?? 0,
                mail.FungibleIdAndCounts?.Count() ?? 0);
        }
    }
}
