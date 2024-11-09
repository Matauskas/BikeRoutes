using System.Net.Mail;
using System.Net;

namespace web_backend
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("bikeciulis@gmail.com", "qygv wndu epzl moxq")
            };

            return client.SendMailAsync(
                new MailMessage(from: "bikeciulis@gmail.com",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
