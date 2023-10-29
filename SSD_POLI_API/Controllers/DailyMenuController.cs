using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace SSD_POLI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyMenuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DailyMenuController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                var email = principal.Identity.Name;


                var emailParts = email.Split('@');
                if (emailParts.Length != 2)
                {
                    return BadRequest("Invalid email address format");
                }

                var domain = emailParts[1].ToLower();

                var user = _context.LoginUser.FirstOrDefault(u => u.Email == email);

                if (user != null)
                {
                    var isUptDomain = domain == "upt.ro" || domain.EndsWith(".upt.ro");

                    var dailyMenu = _context.DailyMenu.ToList();

                    if (dailyMenu == null || dailyMenu.Count == 0)
                    {
                        return NotFound();
                    }

                    var response = new
                    {
                        IsUptDomain = isUptDomain,
                        DailyMenu = dailyMenu
                    };

                    return Ok(response);
                }
                else
                {
                    return NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"JWT validation failed: {ex.Message}");
            }
        }
        [HttpPut("Picture/{id}")]
        public IActionResult Put(int id, [FromForm] DailyMenuModel updatedItem, [FromForm] IFormFile image)
        {
            var dailyMenuItem = _context.DailyMenu.FirstOrDefault(item => item.Id == id);

            if (dailyMenuItem == null)
            {
                return NotFound("Daily menu item not found.");
            }

            dailyMenuItem.Title = updatedItem.Title;
            dailyMenuItem.Description = updatedItem.Description;
            dailyMenuItem.PriceForUPT = updatedItem.PriceForUPT;
            dailyMenuItem.PriceOutsidersUPT = updatedItem.PriceOutsidersUPT;

            if (image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.CopyTo(memoryStream);
                    dailyMenuItem.Picture = memoryStream.ToArray();
                }
            }

            _context.SaveChanges();

            var response = new
            {
                UpdatedItem = dailyMenuItem
            };

            return Ok(response);
        }


    }
}