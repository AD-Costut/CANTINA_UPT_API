using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

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

                // Extract the domain from the email address
                var emailParts = email.Split('@');
                if (emailParts.Length != 2)
                {
                    return BadRequest("Invalid email address format");
                }

                var domain = emailParts[1].ToLower(); // Convert to lowercase for case-insensitive comparison

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

    }
}