using Microsoft.AspNetCore.Mvc;


namespace web_backend.Controllers
{
    public class EmailController : ControllerBase
    {
        private readonly IEmailSender emailSender;

        public EmailController(IEmailSender emailSender)
        {
            this.emailSender = emailSender;
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string subject, string message)
        {
            return await EmailUtility.SendEmailAsync(emailSender, email, subject, message);
        }
    }
}
