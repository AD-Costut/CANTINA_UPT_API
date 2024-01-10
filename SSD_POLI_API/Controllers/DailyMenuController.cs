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

        [HttpPut("CardMenu/{id}")]
        public IActionResult Put(int id, [FromForm] DailyMenuModel updatedItem)
        {
            var dailyMenuItem = _context.DailyMenu.FirstOrDefault(item => item.Id == id);

            if (dailyMenuItem == null)
            {
                return NotFound("Daily menu item not found.");
            }

            dailyMenuItem.Title = updatedItem.Title;
            dailyMenuItem.Description = updatedItem.Description;
            dailyMenuItem.PriceForUPT = updatedItem.PriceForUPT / 100.0f;
            dailyMenuItem.PriceOutsidersUPT = updatedItem.PriceOutsidersUPT / 100.0f;
            dailyMenuItem.Portions = updatedItem.Portions;
         


            _context.SaveChanges();

            var response = new
            {
                UpdatedItem = dailyMenuItem
            };

            return Ok(response);
        }

        [HttpDelete("delete")]
        public IActionResult DeleteDailyMenuItems([FromBody] int[] ids)
        {
            try
            {

                var dailyMenuItemsToDelete = _context.DailyMenu.Where(item => ids.Contains(item.Id)).ToList();
                if (dailyMenuItemsToDelete.Count == 0)
                {
                    return NotFound("No matching daily menu items found for deletion.");
                }

                _context.DailyMenu.RemoveRange(dailyMenuItemsToDelete);
                _context.SaveChanges();

                return Ok("Daily menu items deleted successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting daily menu items: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Post([FromForm] DailyMenuModel newItem, [FromForm] IFormFile image)
        {
            if (newItem == null)
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                var dailyMenuItem = new DailyMenuModel
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
                        dailyMenuItem.Picture = memoryStream.ToArray();
                    }
                }

                _context.DailyMenu.Add(dailyMenuItem);
                _context.SaveChanges();

                var response = new
                {
                    AddedItem = dailyMenuItem
                };

                return CreatedAtAction(nameof(Get), new { id = dailyMenuItem.Id }, response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding daily menu item: {ex.Message}");
            }
        }



    }

}