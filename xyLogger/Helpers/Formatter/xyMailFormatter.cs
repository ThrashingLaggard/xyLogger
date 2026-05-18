using System.Net.Mail;
using System.Text;

namespace xyLogger.Helpers.Formatters
{
    public static class xyMailFormatter
    {

        private const string TimestampLabel = "Timestamp: ";
        private const string FromLabel = "From :";
        private const string SenderLabel = "Sender: ";
        private const string ToLabel = "To: ";
        private const string SubjectLabel = "Subject: ";
        private const string AttachmentsLabel = "AttachmentsCount: ";
        private const string HeaderLabel = "Header: ";
        private const string BodyLabel = "Body: ";
        private const string CcLabel = "Cc:";
        private const string BccLabel = "Bcc: ";

        /// <summary>
        /// Converts a <see cref="MailMessage"/> into a detailed readable log string.
        /// Includes metadata such as sender, recipient, subject, and headers.
        /// </summary>
        /// <param name="mailMessage">The email message to log.</param>
        /// <returns>A formatted string containing email details.</returns>
        public static string FormatMailDetails(MailMessage mailMessage)
        {
            StringBuilder sb = new(512);

            sb.Append(TimestampLabel).Append(' ').AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(FromLabel).Append(' ').AppendLine(mailMessage.From?.Address);
            sb.Append(SenderLabel).Append(' ').AppendLine(mailMessage.Sender?.Address);

            sb.Append(ToLabel).Append(' ')
              .AppendLine(string.Join(", ", mailMessage.To.Select(m => m.Address)));

            if (mailMessage.CC.Count > 0) sb.Append(CcLabel).Append(' ').AppendLine(string.Join(", ", mailMessage.CC.Select(m => m.Address)));

            if (mailMessage.Bcc.Count > 0) sb.Append(BccLabel).Append(' ').AppendLine(string.Join(", ", mailMessage.Bcc.Select(m => m.Address)));

            sb.Append(SubjectLabel).Append(' ').AppendLine(mailMessage.Subject);
            sb.Append("IsBodyHtml: ").AppendLine(mailMessage.IsBodyHtml.ToString());
            sb.Append(BodyLabel).Append(' ').AppendLine(mailMessage.Body);

            sb.Append(AttachmentsLabel).Append(' ').AppendLine(mailMessage.Attachments.Count.ToString());
            if (mailMessage.Attachments.Count > 0)
            {
                foreach (var att in mailMessage.Attachments)
                {
                    sb.Append("  ").AppendLine(att.Name);
                }
            }

            if (mailMessage.Headers.Count > 0)
            {
                sb.AppendLine(HeaderLabel);
                foreach (string key in mailMessage.Headers.Keys)
                {
                    sb.Append("  ").Append(key).Append(": ")
                      .AppendLine(mailMessage.Headers[key]);
                }
            }

            return sb.ToString();
        }

    }
}
