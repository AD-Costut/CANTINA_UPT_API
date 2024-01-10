using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Mail;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;


namespace SSD_POLI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderFoodMachine : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public OrderFoodMachine(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Route("/Daily")]
        [HttpPost]
        public async Task<IActionResult> PostDaily([FromBody] int[] ids)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var dailyMenuItems = _context.DailyMenu.Where(item => ids.Contains(item.Id)).ToList();

            foreach (var dailyMenuItem in dailyMenuItems)
            {

                if (dailyMenuItem.Portions > 0)
                {
                    dailyMenuItem.Portions -= 1;
                }
            }


            _context.SaveChanges();


            try
            {
                var selectedElementsForDailyOrder = _context.DailyMenu.Where(item => ids.Contains(item.Id)).ToList();



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
                var isUptDomain = domain == "upt.ro" || domain.EndsWith(".upt.ro");

                string emailSubject = "Rezervarea comandă CantinaUPT";
                string emailBody = $"Ați comandat:\n\n";
                double orderTotalPrice = 0;

                for (int i = 0; i < selectedElementsForDailyOrder.Count; i++)
                {

                    if (isUptDomain)
                    {
                        emailBody += $"1x {selectedElementsForDailyOrder[i].Title}" + "\n";
                        orderTotalPrice += selectedElementsForDailyOrder[i].PriceForUPT;
                    }
                    else
                    {
                        emailBody += $"1x {selectedElementsForDailyOrder[i].Title}" + "\n";
                        orderTotalPrice += selectedElementsForDailyOrder[i].PriceOutsidersUPT;
                    }

                }

                emailBody += $"\nPreț total: {orderTotalPrice} lei" + "\n\n" + "Vă mulțumim pentru comandă și vă așteptăm să o ridicați până la ora 17:30!";

                await SendEmailAsync(email, emailSubject, emailBody);

                return Ok("Menu added successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest($"JWT validation failed: {ex.Message}");
            }
        }

        [Route("/Standard")]
        [HttpPost]
        public async Task<IActionResult> PostStandard([FromBody] int[] ids)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var standardMenuItems = _context.StandardMenu.Where(item => ids.Contains(item.Id)).ToList();

            foreach (var standardMenuItem in standardMenuItems)
            {
                if (standardMenuItem.Portions > 0)
                {
                    standardMenuItem.Portions -= 1;
                }
            }

            _context.SaveChanges();

            try
            {
                var selectedElementsForStandardOrder = _context.StandardMenu.Where(item => ids.Contains(item.Id)).ToList();

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
                var isUptDomain = domain == "upt.ro" || domain.EndsWith(".upt.ro");

                string emailSubject = "Rezervarea comandă CantinaUPT";
                string emailBody = $"Ați comandat:\n\n";
                double orderTotalPrice = 0;
                for (int i = 0; i < selectedElementsForStandardOrder.Count; i++)
                {
                    if (isUptDomain)
                    {
                        emailBody += $"1x {selectedElementsForStandardOrder[i].Title}" + "\n";
                        orderTotalPrice += selectedElementsForStandardOrder[i].PriceForUPT;
                    }
                    else
                    {
                        emailBody += $"1x {selectedElementsForStandardOrder[i].Title}" + "\n";
                        orderTotalPrice += selectedElementsForStandardOrder[i].PriceOutsidersUPT;
                    }
                }

                emailBody += $"\nPreț total: {orderTotalPrice} lei" + "\n\n" + "Vă mulțumim pentru comandă și vă așteptăm să o ridicați până la ora 17:30!";

                await SendEmailAsync(email, emailSubject, emailBody);

                return Ok("Menu added successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest($"JWT validation failed: {ex.Message}");
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            await Task.Run(() => SendEmail(to, subject, body));
        }

        private void SendEmail(string to, string subject, string body)
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("cantinaupt@gmail.com", "jgbuybjhyveaeskq"),
                EnableSsl = true
            };

            client.Send("cantinaupt@gmail.com", to, subject, body);
        }

    }
}