using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebAppYte.Common
{
    public class SendMail
    {
        public static void Send(string toEmail, string subject, string body)
        {
            var fromEmail = "kiet04uh@gmail.com";
            var password = "cbniaipaywrwzotl";

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(fromEmail, password);
                smtp.EnableSsl = true;

                var fromAddress = new MailAddress(
                    fromEmail,
                    "Trung Tâm Y Tế Quyền Lợi & Sức Khỏe",
                    Encoding.UTF8
                );

                var toAddress = new MailAddress(toEmail);

                using (var message = new MailMessage(fromAddress, toAddress))
                {
                    message.Subject = subject;
                    message.SubjectEncoding = Encoding.UTF8;
                    message.Body = body;
                    message.BodyEncoding = Encoding.UTF8;
                    message.IsBodyHtml = true;

                    smtp.Send(message);
                }
            }
        }
    }
}