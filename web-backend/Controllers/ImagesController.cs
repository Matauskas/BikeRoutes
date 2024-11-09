using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace web_backend.Controllers
{
    [ApiController]
    [Route("/Images")]
    public class ImagesController : ControllerBase
    {
        public class ImagePayload
        {
            [Required]
            public string PhotoUrl { get; set; }
        }

        private readonly IWebHostEnvironment _environment;
        private readonly UserRepository _userRepository;
 

        public ImagesController(IWebHostEnvironment environment, UserRepository userRepository)
        {
            _environment = environment;
            _userRepository = userRepository;
            
        }
        [Authorize]
        [HttpPost("upload")]
        public async Task<ActionResult> UploadImage(IFormFile file)
        {
            try
            {
                string directory = System.IO.Directory.GetCurrentDirectory();
                if (file == null || file.Length == 0)
                {
                    return ErrorReason.BadRequest(new ErrorReason("not_found", "file"));
                }

                if (!IsImageFile(file))
                {
                    return ErrorReason.BadRequest(new ErrorReason("netinkamas_tipas", "file"));
                }
         
                if (file.Length > 4 * 1024 * 1024) 
                {
                    return ErrorReason.BadRequest(new ErrorReason("per_didelis", "file"));
                }

                string uploadsFolder = "uploads";
                
                if (!Directory.Exists(uploadsFolder))
                {                  
                    Directory.CreateDirectory(uploadsFolder);
                }
               
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
           
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string imageUrl = "/uploads/" + uniqueFileName;
               
                var userId = HttpContext.Items["userId"] as int?;
                if (userId == null)
                {                
                    return Unauthorized();
                }

                var user = await _userRepository.GetUserById(userId.Value);
                if (user == null)
                {                
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(user.PhotoUrl))
                {
                   
                    var oldPhotoPath = directory + user.PhotoUrl;
                    Console.WriteLine(oldPhotoPath);
                    if (System.IO.File.Exists(oldPhotoPath))
                    {
                        System.IO.File.Delete(oldPhotoPath);
                    }
                }
                user.PhotoUrl = imageUrl;
                await _userRepository.ChangePhotoUrl(user, imageUrl);
          
                return Ok(imageUrl);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("receive")]
        public async Task< IActionResult> GetUserPhoto(int userId)
        {
            string directory = System.IO.Directory.GetCurrentDirectory();
                 
            string photoUrl = directory + await _userRepository.GetUserProfileImageUrl(userId);
    
            byte[] imageData = System.IO.File.ReadAllBytes(photoUrl); ; 

            return new FileContentResult(imageData, "image/jpeg");
        }
        private bool IsImageFile(IFormFile file)
        {
            if (file.ContentType.ToLower() == "image/jpeg" ||
                file.ContentType.ToLower() == "image/png" ||
                file.ContentType.ToLower() == "image/gif")
            {
                return true;
            }
            return false;
        }

    }

}

  
