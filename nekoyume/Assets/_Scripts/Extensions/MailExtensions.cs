using System.Linq;
using System.Text;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class MailExtensions
    {
        public static string GetPopupContent(
            this UnloadFromMyGaragesRecipientMail mail,
            MaterialItemSheet sheet)
        {
            var sb = new StringBuilder();
            var title = L10nManager.Localize("MAIL_UNLOAD_FROM_MY_GARAGES_RECIPIENT_POPUP_TITLE");
            sb.AppendLine($"<b>{title}</b>");
            sb.AppendLine();
            if (mail.FungibleAssetValues is not null)
            {
                var arr = mail.FungibleAssetValues.ToArray();
                for (var i = 0; i < arr.Length; i++)
                {
                    var (_, value) = arr[i];
                    if (i == 0)
                    {
                        sb.Append($"{value}");
                        continue;
                    }

                    sb.Append($", {value}");
                }
            }

            if (mail.FungibleIdAndCounts is not null)
            {
                sb.Append("\n");
                var arr = mail.FungibleIdAndCounts.ToArray();
                for (var i = 0; i < arr.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append("\n");
                    }

                    var (id, count) = arr[i];
                    if (!sheet.TryGetLocalizedName(id, out var name))
                    {
                        sb.Append($"??({id}) x{count}");
                        continue;
                    }

                    sb.Append($"{name} x{count}");
                }
            }

            return sb.ToString();
        }

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
