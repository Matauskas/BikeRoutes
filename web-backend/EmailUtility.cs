using Microsoft.AspNetCore.Mvc;

namespace web_backend
{
    public static class EmailUtility
    {
        public static async Task<IActionResult> SendEmailAsync(IEmailSender emailSender, string email, string subject, string message)
        {
            try
            {
                await emailSender.SendEmailAsync(email, subject, message);
                var pranesimas = "OK";
                return new OkObjectResult(pranesimas);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error sending email: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
