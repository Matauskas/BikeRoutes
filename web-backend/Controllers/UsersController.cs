using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using TechTalk.SpecFlow.CommonModels;

namespace web_backend.Controllers;

public class ChangePasswordPayload
{
    public string Password { get; set; }
    public string newPassword { get; set; }
}

public class ResetPasswordPayload
{
    public string Password { get; set; }

    public string Token { get; set; }

}

public class PhotoPayload
{
    public string PhotoUrl { get; set; }
}
public class EmailPayload
{
    public string Email { get; set; }
}

public class LoginPayload
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class UserRegisterPayload
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("Username")]
    public string Username { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }
}

public class UpdateCoordinatesPayload
{
    [JsonPropertyName("Username")]
    public string Username { get; set; }

    [JsonPropertyName("Longitude")]
    public string Longitude { get; set; }

    [JsonPropertyName("Latitude")]
    public string Latitude { get; set; }
}

[ApiController]
[Route("/Users")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly IEmailSender emailSender;
    private readonly RoutesRepository routes;
    private readonly byte[] jwtKey;
    private readonly IHttpContextAccessor _httpContextAccessor;
    IWebHostEnvironment env;

    public UsersController(IEmailSender emailSender, UserRepository userRepository, RoutesRepository routeRepository, IWebHostEnvironment env, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        this.emailSender = emailSender;
        _userRepository = userRepository;
        routes = routeRepository;
        this.env = env;
        jwtKey = Convert.FromBase64String(configuration["JWTKey"]);
        _httpContextAccessor = httpContextAccessor;
    }

    [Authorize]
    [HttpGet]
    public List<User> ListUsers()
    {
        return _userRepository.List();
    }

    [HttpPost("login")]
    public async Task<ActionResult<User>> Login([FromBody] LoginPayload credentials)
    {
        var user = _userRepository.GetUserByUsername(credentials.Username);

        var errors = new List<object>();
        if (user == null)
        {
            errors.Add(new ErrorReason("non_existent", "Username"));
        }
        else if (!VerifyPassword(user.Password, credentials.Password))
        {
            errors.Add(new ErrorReason("incorrect", "Password"));
        }

        if (errors.Any())
        {
            return BadRequest(errors);
        }

        // Generate JWT
        var expiresAt = DateTime.UtcNow.AddHours(1); // TODO: Move expiration duration to configuration variable
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }),
            Expires = expiresAt, // Tokeno galiojimo laikas
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(jwtKey), SecurityAlgorithms.HmacSha512Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var serializedToken = tokenHandler.WriteToken(token);

        Response.Cookies.Append("token", serializedToken, new CookieOptions
        {
            HttpOnly = false,
            Expires = expiresAt,
            Secure = !env.IsDevelopment(),
        });

        return user;
    }

    [HttpPost("check_email")]
    public async Task<IActionResult> CheckEmail([FromBody] EmailPayload payload)
    {
        string email = payload.Email;
        var existingUserByEmail = await _userRepository.GetUserByEmailAsync(email);

        if (existingUserByEmail != null)
        {

            var request = _httpContextAccessor.HttpContext.Request.Headers.Origin.ToString();

            var baseUrl = $"{request}";

            var resetToken = GeneratePasswordResetToken(existingUserByEmail);
            var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

            await SendResetPasswordEmail(existingUserByEmail.Email, resetLink);

            return Ok(new { message = "Elektroninis paštas egzistuoja." });
        }

        return NotFound();
    }

    private string GeneratePasswordResetToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
            new Claim("userId", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(jwtKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [HttpPost("reset_password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordPayload payload)
    {
        try
        {
            var errorResponses = new List<ErrorReason>();

            var token = payload.Token;
            var newPassword = payload.Password;

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken securityToken;
            var claimsPrincipal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            var userEmailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);
            if (userEmailClaim == null)
            {
                errorResponses.Add(new ErrorReason("not_found", "email"));
            }

            var userEmail = userEmailClaim.Value;

            var user = await _userRepository.GetUserByEmailAsync(userEmail);
            if (user == null)
            {

                errorResponses.Add(new ErrorReason("not_found", "user"));
            }

            var success = await _userRepository.ResetPassword(user, newPassword);
            if (!success)
            {

                return ErrorReason.BadRequest(
                               new ErrorReason("Reset_Failed"));
            }


            return Ok(new { message = "Password reset successful." });
        }
        catch (SecurityTokenValidationException ex)
        {
            // Token validation failed, return appropriate error response
            return ErrorReason.BadRequest(
               new ErrorReason("Token_Validation_failed"));
        }
        catch (Exception ex)
        {

            return ErrorReason.BadRequest(
               new ErrorReason("sumting_wong"));
        }
    }
    private async Task SendResetPasswordEmail(string email, string resetLink)
    {
        try
        {
            var emailSender = new EmailSender();
            var subject = "Slaptažodžio keitimas";
            var message = $"Mielas vartotojau,\n\nPrašau paspauskite ant šios mėlynos nuorodos, jei norite pakeisti savo slaptažodį:\n{resetLink}";

            await emailSender.SendEmailAsync(email, subject, message);
            Console.WriteLine("Laiškas išsiųstas sėkmingai.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Klaida siunčiant laišką. " + ex.Message);
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterPayload payload)
    {
        var errorResponses = new List<ErrorReason>();

        if (_userRepository.GetUserByUsername(payload.Username) != null)
        {
            errorResponses.Add(new ErrorReason("occupied", "username"));
        }

        if (await _userRepository.GetUserByEmailAsync(payload.Email) != null)
        {
            errorResponses.Add(new ErrorReason("occupied", "email"));
        }

        if (payload.Username.All(char.IsDigit))
        {
            errorResponses.Add(new ErrorReason("all_digits", "username"));
        }

        if (!IsValidPassword(payload.Password))
        {
            errorResponses.Add(new ErrorReason("too_weak", "password"));
        }

        if (errorResponses.Any())
        {
            return BadRequest(errorResponses);
        }

        var user = new User
        {
            Username = payload.Username,
            FirstName = payload.FirstName,
            LastName = payload.LastName,
            Email = payload.Email,
            Password = payload.Password,
        };



        var subject = "Registracija";
        var message = "Jūs sėkmingai prisiregistravote!";

        bool success = _userRepository.RegisterUser(user);

        if (!success)
        {

            return ErrorReason.BadRequest(
               new ErrorReason("sorry_better_luck_next_time")
         );


        }
        else
        {
            var emailResult = await EmailUtility.SendEmailAsync(emailSender, user.Email, subject, message);

            if (emailResult is OkObjectResult)
            {
                Console.WriteLine("Issiusta i {0}", user.Email);
            }

            var response = new
            {
                field = "Registracija",
                reason = "successful"
            };

            return Ok(response); // or return another success status code
        }
    }



    [Authorize]
    [HttpPost("getData")]
    public async Task<ActionResult<User>> GetData()
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return NotFound();

        var user = await _userRepository.GetUserById(userId.Value);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [Authorize]
    [HttpPost("changePsw")]
    public async Task<IActionResult> Change([FromBody] ChangePasswordPayload userPsw)
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return NotFound();

        var user = await _userRepository.GetUserById(userId.Value);
        if (user == null) return NotFound();

        if (!VerifyPassword(user.Password, userPsw.Password))
        {
            return ErrorReason.BadRequest(
                new ErrorReason("incorrect", "Password")
            );
        }

        var success = await _userRepository.ChangeUserPsw(user, userPsw.newPassword);
        if (!success)
        {
            return ErrorReason.BadRequest(
                new ErrorReason("sorry_better_luck_next_time")
            );
        }

        return Ok();
    }

    [Authorize]
    [HttpPost("deleteUser")]
    public async Task<IActionResult> Delete()
    {
        var userId = HttpContext.Items["userId"] as int?;
        if (userId == null) return NotFound();

        var user = await _userRepository.GetUserById(userId.Value);
        if (user == null) return NotFound();

        // Delete the user's photo if it exists
        if (!string.IsNullOrEmpty(user.PhotoUrl))
        {
            string directory = System.IO.Directory.GetCurrentDirectory();
            // Construct the file path from the photo URL and delete the file
            var filePath = directory + user.PhotoUrl;
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        bool success = await _userRepository.DeleteById(userId.Value);
        if (!success) return NotFound();

        return Ok();
    }

    [HttpGet("{idOrUsername}")]
    public async Task<ActionResult<User>> GetUser(string idOrUsername)
    {
        User? user = null;
        if (idOrUsername.All(char.IsDigit))
        {
            user = await _userRepository.GetUserById(int.Parse(idOrUsername));
        }
        else
        {
            user = _userRepository.GetUserByUsername(idOrUsername);
        }

        if (user == null) return NotFound();
        return user;
    }

    [Authorize]
    [HttpGet("{idOrUsername}/routes")]
    public async Task<ActionResult<List<Route>>> ListRoutes(string idOrUsername)
    {
        int userId = 0;
        if (idOrUsername.All(char.IsDigit))
        {
            userId = int.Parse(idOrUsername);
        }
        else
        {
            var user = _userRepository.GetUserByUsername(idOrUsername);
            if (user == null) return NotFound();
            userId = user.Id;
        }

        return await routes.ListByUser(userId);
    }


    [HttpPost("update")]
    [Authorize]
    public async Task<IActionResult> UpdateCoordinates([FromBody] UpdateCoordinatesPayload payload)
    {
        try
        {
            var user = _userRepository.GetUserByUsername(payload.Username);
            if (user == null)
            {
                return NotFound();
            }

            // Check if the coordinates have changed
            if (user.Latitude == payload.Latitude && user.Longitude == payload.Longitude)
            {
                Console.Write("NEPAKITO");
                return Ok(new { message = "Coordinates remain unchanged." });

            }

            var success = await _userRepository.UpdateUserAsync(payload.Username, payload.Latitude, payload.Longitude);
            if (!success)
            {
                return ErrorReason.BadRequest(
                    new ErrorReason("update_failed")
                );
            }

            return Ok(new { message = "Coordinates updated successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while updating coordinates: " + ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("location")]
    // [Authorize]
    public async Task<IActionResult> GetUserLocation(int id)
    {
        try
        {
            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            var location = new { Latitude = user.Latitude, Longitude = user.Longitude };
            return Ok(location);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while retrieving user location: " + ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private bool VerifyPassword(string hashedPassword, string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
    }

    private bool IsValidPassword(string password)
    {
        return password.Length >= 8;
    }
}