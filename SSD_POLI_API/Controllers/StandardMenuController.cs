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
    public class StandardMenuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public StandardMenuController(ApplicationDbContext context, IConfiguration configuration)
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

                    var standardMenu = _context.StandardMenu.ToList();

                    if (standardMenu == null || standardMenu.Count == 0)
                    {
                        return NotFound();
                    }

                    var response = new
                    {
                        IsUptDomain = isUptDomain,
                        StandardMenu = standardMenu
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
        public IActionResult Put(int id, [FromForm] StandardMenuModel updatedItem, [FromForm] IFormFile image)
        {
            var standardMenuItem = _context.StandardMenu.FirstOrDefault(item => item.Id == id);

            if (standardMenuItem == null)
            {
                return NotFound("Daily menu item not found.");
            }

            standardMenuItem.Title = updatedItem.Title;
            standardMenuItem.Description = updatedItem.Description;
            // dailyMenuItem.PriceForUPT = updatedItem.PriceForUPT;
            // dailyMenuItem.PriceOutsidersUPT = updatedItem.PriceOutsidersUPT;

            if (image != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.CopyTo(memoryStream);
                    standardMenuItem.Picture = memoryStream.ToArray();
                }
            }

            _context.SaveChanges();

            var response = new
            {
                UpdatedItem = standardMenuItem
            };

            return Ok(response);
        }

        [HttpPut("CardMenu/{id}")]
        public IActionResult Put(int id, [FromForm] StandardMenuModel updatedItem)
        {
            var standardMenuItem = _context.StandardMenu.FirstOrDefault(item => item.Id == id);

            if (standardMenuItem == null)
            {
                return NotFound("Standard menu item not found.");
            }

            standardMenuItem.Title = updatedItem.Title;
            standardMenuItem.Description = updatedItem.Description;
            standardMenuItem.PriceForUPT = updatedItem.PriceForUPT/100.0;
            standardMenuItem.PriceOutsidersUPT = updatedItem.PriceOutsidersUPT/100.0;
            standardMenuItem.Portions = updatedItem.Portions;


            _context.SaveChanges();

            var response = new
            {
                UpdatedItem = standardMenuItem
            };

            return Ok(response);
        }

        [HttpDelete("delete")]
        public IActionResult DeleteStandardMenuItems([FromBody] int[] ids)
        {
            try
            {

                var standardMenuItemsToDelete = _context.StandardMenu.Where(item => ids.Contains(item.Id)).ToList();
                if (standardMenuItemsToDelete.Count == 0)
                {
                    return NotFound("No matching standard menu items found for deletion.");
                }

                _context.StandardMenu.RemoveRange(standardMenuItemsToDelete);
                _context.SaveChanges();

                return Ok("Standard menu items deleted successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting standard menu items: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Post([FromForm] StandardMenuModel newItem, [FromForm] IFormFile image)
        {
            if (newItem == null)
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                var standardMenuItem = new StandardMenuModel
                {
                    Title = newItem.Title,
                    Description = newItem.Description,
                    PriceForUPT = newItem.PriceForUPT/100.0,
                    PriceOutsidersUPT = newItem.PriceOutsidersUPT/100.0,
                    Portions=newItem.Portions,
                };

                if (image != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        image.CopyTo(memoryStream);
                        standardMenuItem.Picture = memoryStream.ToArray();
                    }
                }

                _context.StandardMenu.Add(standardMenuItem);
                _context.SaveChanges();

                var response = new
                {
                    AddedItem = standardMenuItem
                };

                return CreatedAtAction(nameof(Get), new { id = standardMenuItem.Id }, response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding daily menu item: {ex.Message}");
            }
        } 

    }

}