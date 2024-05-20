using NotifierSystemWorkerConsumer.Models;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NotifierSystemWorkerConsumer
{
    public class EmaiConnector
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;

        public EmaiConnector(AppSettings appSettings, ILogger logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<bool> sendEmail(string email, string subject, string emailText)
        {
            try
            {
                using (SmtpClient client = new SmtpClient())
                using (MailMessage mail = new MailMessage())
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                                     delegate (object s, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                     X509Chain chain, SslPolicyErrors sslPolicyErrors)
                                     { return true; };

                    client.Host = _appSettings.emailHost;
                    client.Port = Convert.ToInt16(_appSettings.emailPort);
                    // client.Timeout = 10000;
                    client.Credentials = new NetworkCredential(_appSettings.emailFrom, _appSettings.emailFromPassword);
                    client.EnableSsl = true;
                    //client.DeliveryMethod = SmtpDeliveryMethod.Network;


                    //client.UseDefaultCredentials = true;

                    mail.From = new MailAddress(_appSettings.emailFrom, "Реестр 2.0");
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = emailText;
                    mail.To.Add(new MailAddress(email));
                    // mail.Attachments.Add(new Attachment(ms2, "EkCreditsReport_" + date_trans.ToString("dd_MM_yyyy") + ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

                    //send the mail

                    client.Send(mail);
                }
                return true;
            }
            catch (Exception ex)
            {
                //логирование в консоль
                _logger.LogError(ex, "Error in email sender");
                return false;
            }
        }

    }
}
