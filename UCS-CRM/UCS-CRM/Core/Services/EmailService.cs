using MimeKit;
using MailKit.Security;
using MailKit.Net.Smtp;
using System.Net.Sockets;

namespace UCS_CRM.Core.Services
{
    public class EmailService : IEmailService
    {
        private IConfiguration _configuration { get; }

   
        private readonly IErrorLogServiceFactory _errorLogRepositoryFactory;

        public EmailService(IConfiguration configuration, IErrorLogServiceFactory errorLogServiceFactory)
        {
            _configuration = configuration;
          
            _errorLogRepositoryFactory = errorLogServiceFactory;
        }

        public void SendEmail(string email, string subject, string HtmlMessage)
        {
            throw new NotImplementedException();
        }

        public string SendMail(string email, string subject, string HtmlMessage)
        {
            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress(_configuration["MailSettings:SenderName"], _configuration["MailSettings:SenderEmail"]);
            message.From.Add(from);

            MailboxAddress to = new MailboxAddress(email, email);
            message.To.Add(to);

            message.Subject = subject;

            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = HtmlMessage;
            //bodyBuilder.TextBody = "Hello World!";

            message.Body = bodyBuilder.ToMessageBody();

            MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();

            // Allow SSLv3.0 and all versions of TLS
            // client.SslProtocols = SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;


            client.Connect("smtp.gmail.com", 587, SecureSocketOptions.Auto);
            client.Authenticate(_configuration["MailSettings:SenderEmail"], _configuration["MailSettings:Password"]);

            client.Send(message);
            client.Disconnect(true);
            client.Dispose();

            /*WebMail.SmtpServer = _configuration["MailSettings:Server"];
            WebMail.SmtpPort = 587;
            WebMail.SmtpUseDefaultCredentials = true;
            WebMail.EnableSsl = true;
            WebMail.UserName = _configuration["MailSettings:SenderEmail"];
            WebMail.Password = _configuration["MailSettings:Password"];

            //WebMail.Send(user.Email, subject, body, null, null, null, true, null, null, null, null, null, null);
             WebMail.Send(to: email, subject: subject, body: HtmlMessage, isBodyHtml: true);*/

            return "Message sent";
        }

        public async Task<KeyValuePair<bool, string>> SendMailWithKeyVarReturns(string email, string subject, string HtmlMessage)
        {
            try
            {
                MimeMessage message = new MimeMessage();

                MailboxAddress from = new MailboxAddress(_configuration["MailSettings:SenderName"], _configuration["MailSettings:SenderEmail"]);
                message.From.Add(from);

                MailboxAddress to = new MailboxAddress(email, email);
                message.To.Add(to);

                message.Subject = subject;

                BodyBuilder bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = HtmlMessage;

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(_configuration["MailSettings:SenderEmail"], _configuration["MailSettings:Password"]);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return new KeyValuePair<bool, string>(true, "Message sent");
            }
            catch (Exception ex)
            {
                string errorMessage = "Message not sent";
                if (ex is SslHandshakeException || (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.HostNotFound))
                {
                    errorMessage = "Message not sent due to internet-related issues. Please try again later.";
                }
                
                var errorServiceFactory = _errorLogRepositoryFactory.Create();
                await errorServiceFactory.LogErrorAsync(ex);


                return new KeyValuePair<bool, string>(false, errorMessage);
            }
        }

        public async Task<KeyValuePair<bool, string>> SendMailWithKeyVarReturn(string email, string subject, string htmlMessage)
        {
            try
            {
                MimeMessage message = new MimeMessage();

                MailboxAddress from = new MailboxAddress(_configuration["MailSettings:SenderName"], _configuration["MailSettings:SenderEmail"]);
                message.From.Add(from);

                MailboxAddress to = new MailboxAddress(email, email);
                message.To.Add(to);

                message.Subject = subject;

                BodyBuilder bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(_configuration["MailSettings:SenderEmail"], _configuration["MailSettings:Password"]);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return new KeyValuePair<bool, string>(true, "Message sent");
            }
            catch (Exception ex)
            {
                string errorMessage = "Message not sent";
                if (ex is SslHandshakeException || (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.HostNotFound))
                {
                    errorMessage = "Message not sent due to internet-related issues. Please try again later.";
                }

                var errorServiceFactory = _errorLogRepositoryFactory.Create();
                await errorServiceFactory.LogErrorAsync(ex);

                return new KeyValuePair<bool, string>(false, errorMessage);
            }
        }

        public async Task SendMailWithKeyVarReturnWrapper(string email, string subject, string htmlMessage)
        {
            var result = await SendMailWithKeyVarReturn(email, subject, htmlMessage);
            // Optionally handle the result here
        }
    }
}
